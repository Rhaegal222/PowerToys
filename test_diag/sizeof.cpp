#include <windows.h>
#include <iostream>

int main() {
    DISPLAYCONFIG_PATH_SOURCE_INFO s;
    DISPLAYCONFIG_PATH_TARGET_INFO t;
    DISPLAYCONFIG_PATH_INFO p;

    std::cout << "SizeOf(LUID) = " << sizeof(LUID) << "\n";
    std::cout << "SizeOf(DISPLAYCONFIG_PATH_SOURCE_INFO) = " << sizeof(DISPLAYCONFIG_PATH_SOURCE_INFO) << "\n";
    std::cout << "SizeOf(DISPLAYCONFIG_PATH_TARGET_INFO) = " << sizeof(DISPLAYCONFIG_PATH_TARGET_INFO) << "\n";
    std::cout << "SizeOf(DISPLAYCONFIG_PATH_INFO) = " << sizeof(DISPLAYCONFIG_PATH_INFO) << "\n";

    std::cout << "\nDISPLAYCONFIG_PATH_SOURCE_INFO Field Offsets:\n";
    std::cout << "  adapterId: Offset = " << (char*)&s.adapterId - (char*)&s << "\n";
    std::cout << "  id: Offset = " << (char*)&s.id - (char*)&s << "\n";
    std::cout << "  modeInfoIdx: Offset = " << (char*)&s.modeInfoIdx - (char*)&s << "\n";
    std::cout << "  cloneGroupId: Offset = " << (char*)&s.cloneGroupId - (char*)&s << "\n";

    std::cout << "\nDISPLAYCONFIG_PATH_INFO Field Offsets:\n";
    std::cout << "  sourceInfo: Offset = " << (char*)&p.sourceInfo - (char*)&p << "\n";
    std::cout << "  targetInfo: Offset = " << (char*)&p.targetInfo - (char*)&p << "\n";
    std::cout << "  flags: Offset = " << (char*)&p.flags - (char*)&p << "\n";

    return 0;
}
