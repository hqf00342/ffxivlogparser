# FF14 log parser

FF14 のログをテキストに変換するテスト.

## 使い方

.net Framework 4.6以降が必要です。（Windows10はプレインストール済）

コマンドプロンプトで以下のようにログファイルかログディレクトリを指定して実行します。ディレクトリを指定した場合は全ログファイルをデコードします。`-v` オプションで削除したバイナリデータも表示します。

```
ffxivlogparser.exe [-v] <ff14 ログファイル>
もしくは
ffxivlogparser.exe [-v] <ff14 ログディレクトリ>
```

例：`00000000.log` を解析し `ff14log.txt`というファイルに保存したい場合。


```
ffxivlogparser.exe 00000000.log > ff14log.txt
```


### Output example

以下のように出力されます。通常1ファイルに1000行入っています。

```
index date  time     [logtype,param] decoded message
----- ----- -------- --------------- -------------------------
0000  11/15 11:39:52 [03,00]         Welcome to <Server Name>!
```


## License

[MIT](https://opensource.org/licenses/MIT)

Copyright (c) 2020 Takayuki Nagashima <hqf00342@nifty.com>