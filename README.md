# RSS generator for DeviantArt group comment wall 

This program scrapes **dA** coments from groups and create RSS feed. So, it's very comfortable to see new comments in RSS reader.

[Download last release](https://github.com/LyrurusTetrix/deviantArt-comments-to-RSS/releases/download/v0.1/dA-comment-to-RSS_BIN_Release-v0.1.zip)

## How to run program

This is console program, so you must put argunents via command line:

  dacrf.exe dA_group_url output.rss [timeout_in_seconds]

Default timeout is 60 seconds, but you can set timeout to any value in second or even turn off timeout if you set timeout to 0 value. 

Example:  

  dacrf.exe https://yurihearts.deviantart.com/ yurihearts.rss 60

[Output RSS file yurihearts.rss](https://github.com/LyrurusTetrix/deviantArt-comments-to-RSS/blob/master/yurihearts.rss)

## Todo

* It must be compatible with profile and picture comments too.
* Downloading progress line



