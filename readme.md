# FF14 log parser

This is a study that decodes the FFXIV's log file into human readable text.  
The Japanese document is [here](readme-j.md).

## Usage

This requires .net Framework 4.6 or later runtime.  
Its runtime is pre-installed on Windows 10.

```
ffxivlogparser.exe [-v] <ff14 log filename>
or
ffxivlogparser.exe [-v] <ff14 log directory>
```

`-v` option shows the trimmed binary message in hexadecimal.

### Output example

```
index date  time     [logtype,param] decoded message
----- ----- -------- --------------- -------------------------
0000  11/15 11:39:52 [03,00]         Welcome to <Server Name>!
```


## License

[MIT](https://opensource.org/licenses/MIT)

Copyright (c) 2020 Takayuki Nagashima <hqf00342@nifty.com>