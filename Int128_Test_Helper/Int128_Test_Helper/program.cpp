#include <iostream>
#include <iomanip>
#include <string>
#include <string_view>
#include <exception>
#include <stdexcept>
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

