# LosslessCut
This Program just calls FFmpeg command line tool like below:
`$ ffmpeg -ss 00:00:00 -t 00:10:00 -i input.mp4 -c:v copy -c:a copy -async 1 output.mp4`
cutting a video/audio file without loss and encoding time.

![](./gui.PNG)

1. Drag&Drop a video/audio file on `[ここに動画ファイルを...]`
2. Set Start and End Time
3. Press `[カット]` Button
