using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MySql.Data.MySqlClient;
using System.Threading;

namespace hwapp
{
    [Route("callback")]
    public class CallbackController : Controller
    {
        public const string SECRET = "test-secret";
        [HttpPost]
        public async Task<IActionResult> Post()
        {
            var stream = new MemoryStream();
            await Request.Body.CopyToAsync(stream);
            stream.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(stream).ReadToEndAsync();
            var nonce = Request.Query["nonce"];
            var timestamp = Request.Query["timestamp"];
            var signature = Request.Headers["X-JDY-Signature"];
            string[] param = { nonce, body, SECRET, timestamp };
            var bytes = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(string.Join(":", param)));
            if (BitConverter.ToString(bytes).Replace("-", "").ToLower().Equals(signature))
            {
                // 开启线程异步处理推送来的数据，防止响应超时
                new Thread(o =>
                {
                    new OrderService(body).Process();
                })
                { IsBackground = true }
                .Start();
                return Ok("success");
            }
            else
            {
                // 因为 aspnetcore 中的 Unauthorized 不能附带消息，所以这里用这个方法
                return new ObjectResult("fail")
                {
                    StatusCode = 401
                };
            }
        }
    }

    public class OrderService
    {
        private MySqlConnection conn;
        private JObject payload;
        public OrderService(string body)
        {
            payload = (JObject)JsonConvert.DeserializeObject(body);
            InitConnection();
        }
        private MySqlConnection InitConnection()
        {
            string constr = "Server=127.0.0.1;Port=3306;Database=webhook;User Id=root;Password=123456;charset=utf8;SslMode=None";
            conn = new MySqlConnection(constr);
            return conn;
        }

        public void Process()
        {
            JObject data = payload["data"].ToObject<JObject>();
            switch ((string) payload["op"])
            {
                case "data_create":
                    Add(data);
                    break;
                case "data_update":
                    Update(data);
                    break;
                case "data_remove":
                    Remove(data);
                    break;
                default:
                    break;
            }
        }

        public void Add(JObject data)
        {
            conn.Open();
            // 将推送数据的对应字段取出来
            string id = data["_id"].ToString();
            string time = data["_widget_1515649885212"].ToString();
            string types = data["_widget_1516945244833"].ToString();
            string address = data["_widget_1516945244846"].ToString();
            string orderItems = data["_widget_1516945244887"].ToString();
            string price = data["_widget_1516945245257"].ToString();
            string addStr = string.Format("insert into `order` values('{0}', '{1}', '{2}', '{3}', '{4}', '{5}')", id, time, types, address, orderItems, price);
            MySqlCommand cmd = new MySqlCommand(addStr, conn);
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public void Update(dynamic data)
        {
            conn.Open();
            // 将推送数据的对应字段取出来
            string id = data["_id"].ToString();
            string time = data["_widget_1515649885212"].ToString();
            string types = data["_widget_1516945244833"].ToString();
            string address = data["_widget_1516945244846"].ToString();
            string orderItems = data["_widget_1516945244887"].ToString();
            string price = data["_widget_1516945245257"].ToString();
            string updateStr = string.Format("update `order` set time='{0}', types='{1}', address='{2}', orderItems='{3}', price='{4}' where id = '{5}'",
                time, types, address, orderItems, price, id);
            MySqlCommand cmd = new MySqlCommand(updateStr, conn);
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public void Remove(dynamic data)
        {
            conn.Open();
            string id = data["_id"];
            string delStr = string.Format("delete from `order` where id = '{0}'", id);
            MySqlCommand cmd = new MySqlCommand(delStr, conn);
            cmd.ExecuteNonQuery();
            conn.Close();
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseUrls("http://*:3100")
                .UseKestrel()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }

    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMvc();
        }
    }

}
