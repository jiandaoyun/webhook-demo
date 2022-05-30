<?php

    define(MYSQL_HOST, 'localhost');
    define(MYSQL_USER, 'root');
    define(MYSQL_PW, '123456');
    define(MYSQL_DB, 'webhook');

    function getSignature ($nonce, $payload, $secret, $timestamp) {
        $content = $nonce.":".$payload.":".$secret.":".$timestamp;
        return sha1($content);
    }

    $payload = file_get_contents("php://input");
    if (strcmp($_SERVER["HTTP_X_JDY_SIGNATURE"], getSignature($_GET["nonce"], $payload, "test-secret", $_GET["timestamp"])) !== 0) {
        header("HTTP/1.1 401 Unauthorized");
        echo "fail";
    } else {
        echo "success";
        handleData($payload);
    }
    // 处理推送来的数据
    function handleData ($payload) {
        // 将字符串解析为JSON
        $payloadJSON = json_decode($payload);
        $data = $payloadJSON -> data;
        switch ($payloadJSON -> op) {
            case 'data_create':
                add($data);
                break;
            case 'data_update':
                update($data);
                break;
            case 'data_remove':
                remove($data);
                break;
            default:
                break;
        }
    }

    function getConnection () {
        // 连接mysql数据库
        $conn = mysqli_connect(MYSQL_HOST,MYSQL_USER,MYSQL_PW,MYSQL_DB, 3306) or die('Unable to connect');;
        // 设置数据编码
        mysqli_query($conn, "set names 'utf8'");
        return $conn;
    }

    function add ($data) {
        $id = $data -> _id;
        $time = $data -> _widget_1515649885212;
        $types = json_encode($data -> _widget_1516945244833, JSON_UNESCAPED_UNICODE);
        $address = json_encode($data -> _widget_1516945244846, JSON_UNESCAPED_UNICODE);
        $orderItems = json_encode($data -> _widget_1516945244887, JSON_UNESCAPED_UNICODE);
        $price = $data -> _widget_1516945245257;
        $conn = getConnection();
        $sql = "insert into `order` values ('".$id."', '".$time."', '".$types."', '".$address."', '".$orderItems."', '".$price."')";
        mysqli_query($conn, $sql);
        mysqli_close($conn);
    }

    function update ($data) {
        $id = $data -> _id;
        $time = $data -> _widget_1515649885212;
        $types = json_encode($data -> _widget_1516945244833, JSON_UNESCAPED_UNICODE);
        $address = json_encode($data -> _widget_1516945244846, JSON_UNESCAPED_UNICODE);
        $orderItems = json_encode($data -> _widget_1516945244887, JSON_UNESCAPED_UNICODE);
        $price = $data -> _widget_1516945245257;
        $conn = getConnection();
        $sql = "update `order` set time = '".$time."', types = '".$types."', address = '".$address."', orderItems = '".$orderItems."',
             price = '".$price."' where id = '".$id."'";
        mysqli_query($conn, $sql);
        mysqli_close($conn);
    }

    function remove ($data) {
        $id = $data -> _id;
        $conn = getConnection();
        $sql = "delete from `order` where id = '".$id."'";
        mysqli_query($conn, $sql);
        mysqli_close($conn);
    }
?>