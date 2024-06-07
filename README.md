# SslCertHub

SslCertHub 是一个 SSL 证书管理工具，它能帮助开发者集中申请和管理 SSL 证书，并自动更新到云服务商。

## 功能

- 申请 SSL 证书
- 管理 SSL 证书
- 自动更新 SSL 证书到云服务商

## 如何运行

1. 克隆项目到本地

```bash
git clone https://github.com/leoskey/SslCertHub.git
```

2. 使用 JetBrains Rider 打开项目

3. 在 `appsettings.json` 文件中配置你的证书提供商和 DNS 提供商的信息

```json
{
  "CertProvider": {
    "LetsEncrypt": {
      "Email": "your-email@example.com"
    }
  },
  "CloudProvider": {
    "AlibabaCloud": {
      "AccessKeyId": "your-access-key-id",
      "AccessKeySecret": "your-access-key-secret"
    }
  },
  "Plugin": {
    "AlibabaCloudCas": {}
  },
  "Domains": [
    {
      "DomainName": "your-domain.com",
      "Plugins": [
        "AlibabaCloudCas"
      ]
    }
  ]
}
```
请注意，你需要将上述配置中的 "your-email@example.com", "your-access-key-id", "your-access-key-secret" 和 "your-domain.com" 替换为你自己的信息。

4. 运行项目

## 如何使用

修改配置后，运行项目，他会自动检查并更新证书

## 注意事项

- 请确保你的证书提供商和 DNS 提供商的 API 密钥是正确的。
- `SslCertManager` 在生成证书后会自动删除 DNS 记录，请确保你的 DNS 提供商支持这个功能。

## 贡献

如果你有任何问题或者建议，欢迎提交 issue 或者 pull request。

## 许可证

本项目采用 MIT 许可证，详情请见 LICENSE 文件。
