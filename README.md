# AspCoreETagCacher
ASP Core Output Cache with Memory Cache and Redis Cache support 

# What the project does
The project contains three types of caching attributes which allow you to add cache to your web core application in a super easy way.
1. ResponseEtagCache //cache only on client side
2. MemoryEtagCache //cache in memory and if you choose also on client side
3. RedisEtagCache //cache in a distributed cache and on client side, the sample web application contain an example with Redis 

To use one of cachers, you just have to add an atriibute to a controller method 
[MemoryEtagCache(ClientSideDuration = 90, ServerSideDuration = 900)]
        
All cachers and middleware support ETags -in other words, the server will return status code 304 instead of data each time when the client and server ETags match. If you are wondering right now how ETag works... When you request some data from a server for the first time the server return data with generated ETag, when you request the same data in the future, your browser will send previously received ETag. Based on this server "knows" when the client needs new data and when the status 304 is enough.
Here you can find some further information what is an eTag https://en.wikipedia.org/wiki/HTTP_ETag

To use caching attribute it's required to add caching middleware. The purpose of CacheETagMiddleware is to intercept request, and provide custom stream to which further middlewares could write (and read).
app.UseMiddleware<CacheETagMiddleware>();

I plan to add installation by bower and better documentation in the next few days. If you have any questions/suggestions don't hesitate to text me.  

## Donation
If this project help you reduce time to develop, you can give me a cup of coffee :)

[![paypal](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=BA4EV8ALWCVYE&lc=US&item_name=ETagCache&no_note=0&cn=Dodaj%20specjalne%20instrukcje%20dla%20sprzedaj%c4%85cego%3a&no_shipping=2&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donateCC_LG%2egif%3aNonHosted)
