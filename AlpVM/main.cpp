#include <stdio.h>
#include <string>

#include <SDL.h>

#include "Debugger.h"

#include "VirtualMachine.h"

#define RGB(r,g,b) ((r << 16) | (g << 8) | (b))

VirtualMachine vm;

bool isRunning = false;
bool debug = false;

int vmThread(void* data)
{
	if (debug)
	{
		Debugger* debugger = new Debugger(&vm);
		bool success = debugger->listen(5566);
		if (!success)
		{
			delete debugger;
			return 0;
		}
		vm.pause();

		while (isRunning)
		{
			debugger->process();

			//vm.executeInstruction();

			//vm.sendKey(16777236);
		}

		delete debugger;
	}
	else
	{
		while (isRunning)
		{
			if (!vm.isPaused())
			{
				vm.executeInstruction();
			}
		}
	}
	return 0;
}

/*
int rgb(Uint8 r, Uint8 g, Uint8 b)
{
	return (r << 16) | (g << 8) | (b);
}

void fillScreen(Uint32 *screen, int fromLine, int totalLine, Uint32 number)
{
	int i = fromLine * 100;
	int size = i + totalLine * 100;
	for (;i < size;++i)
	{
		screen[i] = number;
	}
}

int vmThread(void* data)
{
	VirtualMachine* v = (VirtualMachine*)data;

	UByte* mem = v->getMemory();
	Uint32* video = (Uint32*)(mem + 1024);
	Uint32* screen1 = (Uint32*)(mem + 1036);
	Uint32* screen2 = (Uint32*)(mem + 65536);

	video[0] = 1036;

	UInt32 red = rgb((Uint8)255, 0, 0);
	Uint32 green = rgb(0, (Uint8)255, 0);
	fillScreen(screen1, 0, 60, red);
	fillScreen(screen1, 60, 60, green);
	//fillScreen(screen2, rgb(0, 0, (char)255));
	
	while(1)
	{
		if (video[0] == 1036)
		{
			video[0] = 65536;
		}
		else
		{
			video[0] = 1036;
		}
	}

	return 0;
}
*/
int main(int argc, char* argv[])
{
	{
	int a = 1;
	char * p = (char*)&a;
	char * p2 = p + 1;
	int b;
	*p2 = 1;
	b = a;
}
	int width = 120;
	int height = 100;

	int windowWidth = 640;
	int windowHeight = 480;

	if (argc < 2)
	{
		return 1;
	}
	for (int i = 0; i < argc; ++i)
	{
		if (strcmp("debug", argv[i]) == 0)
		{
			debug = true;
			break;
		}
	}

	const char* program = argv[1];

	
	if (!vm.openFromFile(program))
	{
		return 1;
	}
	vm.restart();

	UByte* memory = vm.getMemory();
	UByte* videoMem = vm.getVideoMemory();
	int* mVideoMemory = (int*)videoMem;
	mVideoMemory[1] = width; //default graphic width
	mVideoMemory[2] = height; //default graphic height

	SDL_Window *window;                    // Declare a pointer

	SDL_Init(SDL_INIT_VIDEO);              // Initialize SDL2

	// Create an application window with the following settings:
	window = SDL_CreateWindow(
		"AlpVM",                  // window title
		SDL_WINDOWPOS_UNDEFINED,           // initial x position
		SDL_WINDOWPOS_UNDEFINED,           // initial y position
		windowWidth,                               // width, in pixels
		windowHeight,                               // height, in pixels
		SDL_WINDOW_OPENGL | SDL_WINDOW_RESIZABLE                 // flags - see below
		);

	// Check that the window was successfully created
	if (window == NULL) {
		// In the case that the window could not be made...
		printf("Could not create window: %s\n", SDL_GetError());
		return 1;
	}

	SDL_Surface* screen = SDL_GetWindowSurface(window);

	// The window is open: could enter program loop here (see SDL_PollEvent())

	Uint32 rmask = 0;
	Uint32 gmask = 0;
	Uint32 bmask = 0;
	Uint32 amask = 0;

	SDL_Surface* surface = SDL_CreateRGBSurface(0, width, height, 32, rmask, gmask, bmask, amask);
	SDL_SetSurfaceBlendMode(surface, SDL_BLENDMODE_NONE);

	isRunning = true;
	SDL_Thread *thread = SDL_CreateThread(vmThread, "VM Thread", (void *)&vm);

	SDL_Event event;

	Uint32* data = (Uint32*)surface->pixels;

	SDL_Rect sourceRect;
	sourceRect.h = height;
	sourceRect.w = width;
	sourceRect.x = 0;
	sourceRect.y = 0;

	SDL_Rect screenRect;
	screenRect.h = screen->h;
	screenRect.w = screen->w;
	screenRect.x = 0;
	screenRect.y = 0;
	
	while (true)
	{
		memcpy(data, memory + mVideoMemory[0], sizeof(Int32)* width * height);

		SDL_UpperBlitScaled(surface, &sourceRect, screen, &screenRect);

		

		SDL_PollEvent(&event);

		if (event.type == ::SDL_QUIT)
		{
			break;
		}
		else if (event.type == SDL_WINDOWEVENT)
		{
			if (event.window.event == SDL_WINDOWEVENT_RESIZED)
			{
				screen = SDL_GetWindowSurface(window);
				screenRect.h = screen->h;
				screenRect.w = screen->w;
			}
		}

		if (width != mVideoMemory[1] || height != mVideoMemory[2])
		{
			width = mVideoMemory[1];
			height = mVideoMemory[2];

			SDL_FreeSurface(surface);
			surface = SDL_CreateRGBSurface(0, width, height, 32, rmask, gmask, bmask, amask);
			data = (Uint32*)surface->pixels;
			SDL_SetSurfaceBlendMode(surface, SDL_BLENDMODE_NONE);
		}

		
		

		SDL_UpdateWindowSurface(window);
	}
	isRunning = false;
	SDL_WaitThread(thread, NULL);

	// Close and destroy the window
	SDL_DestroyWindow(window);

	// Clean up
	SDL_Quit();
	return 0;
}