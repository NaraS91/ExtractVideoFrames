!! FFMediaToolkit library required !!

Before using, change the path to ffmpeg binaries in the program main function.

2 consol functions:
extract [path to folder with video] [video name with extension] 
  output will be in a folder called temp located in the same folder as video file
  
scale [path to the folder with the temp folder] pixelRate
  scales each frame with pixelRate (i.e 1920x1080 frame with pixaleRate 2 scales to 960x540)
  

