#include <iostream>
#include <iomanip>
#include "helper.hpp"

int main(int argc, char* argv[])
{
	try
	{
		return cjm::execute(argc, argv);
	}
	catch (...)
	{
		try
		{
			std::cerr << "Application terminating." << std::endl;
		}
		catch (...)
		{
			
		}
		return -1;
	}
}

