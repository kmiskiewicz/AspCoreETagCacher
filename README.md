# AspCoreETagCacher
ASP Core Output Cache with Memory Cache and Redis Cache support 

# What the project does
The project contains three types of caching attribiutes which allow you to use add cache to your app in a super easy way.
1. ResponseEtagCache //cache only on client side
2. MemoryEtagCache //cache in memmory and if you choose also on client side
3. RedisEtagCache //cache in a distributed cache and on client side, the sample web application contain an example with Redis 
 
All cachers support ETags -in other words, the server return status code 304 and do not send data to client (because browsers take data from the cache)
Here you can find some information what is eTag https://en.wikipedia.org/wiki/HTTP_ETag

To use caching attribiute it's require to add caching middleware. The purpose of CacheETagMiddleware is to intercept request, and provide custom stream to which further middlewares could write (and read).
app.UseMiddleware<CacheETagMiddleware>();

The moddleware calculate ETag for all your requests, 
## Donation

If this project help you reduce time to develop, you can give me a cup of coffee :)...I live in a quite poor country :-/

[![paypal](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=BA4EV8ALWCVYE&lc=US&item_name=ETagCache&no_note=0&cn=Dodaj%20specjalne%20instrukcje%20dla%20sprzedaj%c4%85cego%3a&no_shipping=2&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donateCC_LG%2egif%3aNonHosted)
