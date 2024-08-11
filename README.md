## TPLink C# Lib

Unofficial TP-Link Lib to Read and Send SMS with TP-Link Archer MR600.

Based on projects :
- https://github.com/plewin/tp-link-modem-router
- https://github.com/hercule115/TPLink-Archer


# Read the docs

[Online docs](https://frapp42.github.io/TP-Link/)

## Example :

```C#
using FrApp42.TPLink;

...

Client client = new Client(url, username, password);
client.Send("0606060606", $"Hello {DateTime.Now}");
```