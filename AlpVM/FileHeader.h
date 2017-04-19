#ifndef FILEHEADER_H
#define FILEHEADER_H

#include "Globals.h"

struct FileHeader
{
	UInt32 MagicNumber;
    UInt32 FileFormatVersion;
    UInt32 FileType;
    UInt32 CodeSize;
    UInt32 DataSize;
    UInt32 LitSize;
    UInt32 BssSize;
    UInt32 SymbolDefinitionTableSize;
    UInt32 SymbolUsageTableSize;
};

#endif //FILEHEADER_H