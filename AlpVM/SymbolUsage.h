#ifndef SYMBOLUSAGE_H
#define SYMBOLUSAGE_H

#include <fstream>
#include <string>

class SymbolUsage
{
public:
	enum EnSection
    {
        Data,
        Bss,
        Lit,
        Code
    };

	SymbolUsage();
	~SymbolUsage();

	static SymbolUsage deserialize(std::ifstream& inFile);

	int calculateSize();

	int NameLength;
    EnSection Section;
    int Address;
};

#endif //SYMBOLUSAGE_H