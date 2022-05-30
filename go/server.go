package main

import (
	"crypto/sha1"
	"encoding/hex"
	"io/ioutil"
	"net/http"
	"encoding/json"
    _ "github.com/go-sql-driver/mysql"
	"database/sql"
)

// mysql数据库连接字符串
var CONNECT_STR = "root:123456@tcp(127.0.0.1:3306)/webhook?charset=utf8"
var db *sql.DB

type Order struct {
	Id string
	Time string
	Types string
	Address string
	OrderItems string
	Price float64
}

func getSignature(nonce string, payload string, key string, timestamp string) string {
	signStr := []byte(nonce + ":" + payload + ":" + key + ":" + timestamp)
	sha1Bytes := sha1.Sum(signStr)
	return hex.EncodeToString(sha1Bytes[:])
}

func handler(w http.ResponseWriter, req *http.Request) {
	body, err := ioutil.ReadAll(req.Body)
	if err != nil {
		panic(err)
	}
	req.ParseForm()
	nonce := req.Form["nonce"]
	timestamp := req.Form["timestamp"]
	if req.Header.Get("x-jdy-signature") != getSignature(nonce[0], string(body[:]), "test-secret", timestamp[0]) {
		w.WriteHeader(http.StatusUnauthorized)
		w.Write([]byte("fail"))
		return
	}
	var data map[string]interface{}
	json.Unmarshal(body, &data)
	go handleData(data)
	w.Write([]byte("success"))
}

func main() {
	initDatabase()
	http.HandleFunc("/callback", handler)
	http.ListenAndServe(":3100", nil)
	select {}
}
 
/**
 * 对推送来的数据进行处理
 * @param  {JSON} body 推送来的数据
 */
func handleData(body map[string]interface{}) {
	switch (body["op"]) {
		case "data_create":
			add(process(body["data"]))
			break
		case "data_update":
			update(process(body["data"]))
			break
		case "data_remove":
			remove(body["data"])
			break
		default:
			break
	}
}

/**
 * 将json对应的结构体类型数据转换为对应数据库结构的结构体数据
 * @param {map} data
 * return {struct} order
 */
func process (data interface{}) Order {
	var value = data.(map[string]interface{})
	var id = value["_id"]
	var time = value["_widget_1515649885212"]
	// 在这里对于array和json类型的数据序列化为字符串处理
	var types, _ = json.Marshal(value["_widget_1516945244833"])
	var address, _ = json.Marshal(value["_widget_1516945244846"])
	var orderItems, _ = json.Marshal(value["_widget_1516945244887"])
	var price = value["_widget_1516945245257"]
	var order Order
	order.Id = id.(string)
	order.Time = time.(string)
	order.Types = string(types[:])
	order.Address = string(address[:])
	order.OrderItems = string(orderItems[:])
	if price != nil {
		order.Price = price.(float64)
	} else {
		order.Price = 0
	}
	return order
}

func initDatabase () {
	var err error
	db, err = sql.Open("mysql", CONNECT_STR)
	if err != nil {
		return
	}
	db.SetMaxOpenConns(100)
	db.SetMaxIdleConns(20)
	db.Ping()
}

func add (data Order) {
	db.Exec("insert into `order` values (?, ?, ?, ?, ?, ?)", data.Id, data.Time, data.Types, data.Address, data.OrderItems, data.Price)
}

func update (data Order) {
	db.Exec("update `order` set time = ?, types = ?, address = ?, orderItems = ?, price = ? where id = ?", data.Time, data.Types, data.Address, data.OrderItems, data.Price, data.Id)
}

func remove (data interface{}) {
	var value = data.(map[string]interface{})
	var id = value["_id"].(string)
	db.Exec("delete from `order` where id = ?", id)
}