# Video
Video device of AlpVM is controlled by 12 bytes starting from the address 1024 of memory. It supports accesing pixels directly.
It does not support a text mode yet.

### Video Device Control Structure
|Memory Address|Description|
|--------------|-----------|
|1024          |Contains the memory address where the video device should render. This can be changed to have back buffer or multiple offscreen devices.  
|1028          |Width in pixels
|1032          |Height in pixels

Drawing to a back buffer and then switching to it is preferred way to do drawing in AlpVM. Switching is an atomic operation.
