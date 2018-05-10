# RSS generator for DeviantArt group comment wall 

This program scrapes **dA** coments from groups and create RSS feed. So, it's very comfortable to see new comments in RSS reader.

## How to run program

This is console program, so you must put argunents via command line:

  dacrf.exe dA_group_url output.rss [timeout_in_seconds]

Default timeout is 60 seconds, but you can set timeout to any value in second or even turn off timeout if you set timeout to 0 value. 

Exmpample:
  dacrf.exe https://yurihearts.deviantart.com/ yurihearts.rss 60

## Todo

* It must be compatible with not only dA group comment, but profile & picture comments too.
* Downloading progress line



