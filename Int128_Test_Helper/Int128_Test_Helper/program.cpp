#include <iostream>
#include <iomanip>
#include "helper.hpp"
#include "tests.hpp"
int main(int argc, char* argv[])
{
	try
	{
		cjm::tests::run_tests();
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

