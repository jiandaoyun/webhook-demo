'use strict';
const crypto = require('crypto');
const http = require('http');
const express = require('express');
const bodyParser = require('body-parser');
const mysql = require('mysql');
const _ = require('lodash');

const app = express();
app.use(bodyParser.json());

let db_config = {
    host: 'localhost',
    user: 'root',
    password: '123456',
    database: 'webhook',
    port: 3306
};
const pool = mysql.createPool(db_config);

const getSignature = (nonce, payload, secret, timestamp) => {
    const content = [nonce, payload, secret, timestamp].join(':');
    return crypto.createHash('sha1')
        .update(content, 'utf8')
        .digest('hex');
};

app.set('port', 3100);
app.post('/callback', (req, res) => {
    const data = req.body;
    const payload = JSON.stringify(data);
    const nonce = req.query.nonce;
    const timestamp = req.query.timestamp;
    if (req.headers['x-jdy-signature'] !== getSignature(nonce, payload, 'test-secret', timestamp)) {
        return res.status(401).send('fail');
    }
    handle(data);
    return res.send('success');
});

const server = http.Server(app);
server.listen(app.get('port'), () => {});

const handle = (payload) => {
    const order = process(payload.data);
    switch (payload.op) {
        // 新数据提交
        case 'data_create':
            add(order);
            break;
        // 数据修改
        case 'data_update':
            update(order);
            break;
        // 数据删除
        case 'data_remove':
            remove(payload.data);
            break;
        default:
            break;
    }
};

const process = (data) => {
    return {
        id: _.get(data, '_id', ''),
        time: _.get(data, '_widget_1515649885212', ''),
        types: JSON.stringify(_.get(data, '_widget_1516945244833', '')),
        address: JSON.stringify(_.get(data, '_widget_1516945244846', '')),
        orderItems: JSON.stringify(_.get(data, '_widget_1516945244887', '')),
        price: _.get(data, '_widget_1516945245257', 0),
    };
};

/**
 * 添加数据到数据库
 */
const add = (order) => {
    pool.getConnection((err, conn) => {
        if (!err) {
            conn.query('insert into `order` set ?', order, () => conn.release());
        }
    });
};

/**
 * 更新数据库中的数据
 */
const update = (order) => {
    pool.getConnection((err, conn) => {
        if (!err) {
            let sets = [];
            // 生成更新语句
            _.forIn(_.omit(order, 'id'), (value, key) => {
                sets.push(`${ key }='${ value }'`);
            });
            conn.query(`update \`order\` set ${ sets.join(',') } where id = '${ order.id }'`, order, () => conn.release());
        }
    });
};

/**
 * 移除数据库中的数据
 */
const remove = (data) => {
    pool.getConnection((err, conn) => {
        if (!err) {
            conn.query(`delete from \`order\` where id = '${ data._id }'`, () => conn.release());
        }
    });
};
