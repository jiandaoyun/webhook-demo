# 简道云 Webhook 接收演示

此项目包含各语言环境下，接受简道云 Webhook 回调请求并验证签名的简单演示工程。默认服务端启动在 `3100` 端口，签名使用的默认密钥为 `test-secert`。

## 签名验证流程

1. 为了防止 webhook 接收接口被第三方恶意攻击，用户在开发回调接口时，建议对回调请求进行签名校验，以确保回调请求来源来自于简道云。
2. 获取 POST 请求体 body 内容，序列化为计算签名使用的 payload
3. 获取请求参数中的nonce和timestamp
4. 将 payload 与签名密钥 secret 按照 "\<nonce>:\<payload>:\<secret>:\<timestamp>" 的形式组合为校验字符串 content
5. 以 utf-8 编码形式计算 content 的 sha-1 散列
6. 将 content 散列的十六进制字符串与 POST 请求 header 中的 'X-JDY-Signature' 做比较
7. 若比较结果相同，则通过签名验证；若比较结果不同，则无法通过检查

演示工程直接使用 PHP 提供的原生模块实现，经过 PHP 5.3/5.4 环境测试。

启动运行
将 `php/server.php` 放入相应的网站目录，并访问其路径即可。