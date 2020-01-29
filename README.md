# IIS-ActivePassive-Failover

A service that uses an IIS reverse proxy to provide good-enough-for-non-prod active-passive failover of a web service.

## Test Harness
While the main application is a Windows service, the Test harness allows easily running the application from source by simply providing a basic service framework mock.