# RestSharper

A tiny wrapper around RestSharp to make rest calls less verbose:

```
var request = new RestSharper("https://api.server.com")
                .Url("books")
                .Proxy("127.0.0.1:8080")
                .Headers(("Authorization", "Bearer myToken")
                       , ("another header", "header value"));

            var response = await request.PostAsync();
```
