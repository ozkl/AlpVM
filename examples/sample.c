unsigned int* VideoDevice = (unsigned int*)1024;
unsigned int* Screen = (unsigned int*)4096;

void main()
{
	int r = 0;
	int c = 0;
	
	VideoDevice[0] = (unsigned int)Screen;
	
	for (r = 0; r < 100; ++r)
	{
		for (c = 0; c < 120; ++c)
		{
			Screen[r * 120 + c] = 255;
		}
	}

	while (1);
}
