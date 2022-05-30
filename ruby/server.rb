require 'digest/sha1'
require 'sinatra'
require 'json'
require 'mysql2'

configure do
    set :port, 3100
end

def get_signature(nonce, payload, secret, timestamp)
    Digest::SHA1.hexdigest(nonce + ':' + payload + ':' + secret + ':' + timestamp)
end

post '/callback' do
    request.body.rewind
    payload = request.body.read
    nonce = request.params['nonce']
    timestamp = request.params['timestamp']
    if request.env["HTTP_X_JDY_SIGNATURE"] != get_signature(nonce, payload, 'test-secret', timestamp)
        status 401
        'fail'
    else
        Thread.new() {
            handle(JSON.parse(payload))
        }
        'success'
    end
end

def handle (payload)
    if payload['op'] == 'data_create'
        add(payload['data'])
    end
    if payload['op'] == 'data_update'
        update(payload['data'])
    end
    if payload['op'] == 'data_remove'
        remove(payload['data'])
    end
rescue Exception => e
    p e.message
    p e.backtrace.inspect
end

def get_client
    client = Mysql2::Client.new(
        :host     => '127.0.0.1',
        :username => 'root',
        :password => '1393199906',
        :database => 'webhook',
        :encoding => 'utf8'
    )
    return client
end

def add (data)
    client = get_client
    id = data['_id']
    time = data['_widget_1515649885212']
    types = data['_widget_1516945244833'].to_json
    address = data['_widget_1516945244846'].to_json
    order_items = data ['_widget_1516945244887'].to_json
    price = data['_widget_1516945245257']
    sql = "insert into `order` values ('#{ id }', '#{ time }', '#{ types }', '#{ address }', '#{ order_items }', '#{ price }')"
    client.query(sql)
end

def update (data)
    client = get_client
    id = data['_id']
    time = data['_widget_1515649885212']
    types = data['_widget_1516945244833'].to_json
    address = data['_widget_1516945244846'].to_json
    order_items = data ['_widget_1516945244887'].to_json
    price = data['_widget_1516945245257']
    sql = "update `order` set time = '#{ time }', types = '#{ types }', address = '#{ address }', orderItems = '#{ order_items }', price = '#{ price }' where id = '#{ id }'"
    client.query(sql)
end

def remove (data)
    client = get_client
    id = data['_id']
    sql = "delete from `order` where id = '#{ id }'"
    client.query(sql)
end
