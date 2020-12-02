#include "tests.hpp"

void cjm::test::cjm_assert(bool condition, fsv_t message)
{
	if (!condition) throw cjm::test::cjm_test_fail{ cjm::fstr_t{message} };
}

void cjm::test::cjm_deny(bool condition, fsv_t message)
{
	if (condition) throw cjm::test::cjm_test_fail{ cjm::fstr_t{message} };
}

void cjm::tests::run_tests()
{
	std::cout << "Beginning unit tests: " << newl;
	fsv_t test_name = "test_serialize"sv;
	try
	{
		std::cout << "BEGIN " << test_name << " TEST: " << newl;
		std::int64_t high = 0xc0de'd00d'fea2'b00b;
		std::uint64_t low = 0xc0de'd00d'fea2'b00b;
		int128_t test = absl::MakeInt128(high, low);
		test_serialize(test);
		std::cout << "END " << test_name << " TEST: " << newl;
	}
	catch (const cjm::test::cjm_test_fail& ex)
	{
		std::cerr << "Test [" << test_name << "] FAILED: [" << ex.what() << "]." << newl;
		throw;
	}
	
}

void cjm::tests::test_serialize(int128_t serialize_me)
{
	try
	{
		tstr_t text = serialize(serialize_me);
		int128_t round_tripped = deserialize(text);
		cjm::test::cjm_assert(round_tripped == serialize_me, "round tripped value does not equal original!");
	}
	catch (const cjm::test::cjm_test_fail&)
	{
		throw;
	}
	catch (const std::exception& ex)
	{
		throw cjm::test::cjm_test_fail{ fstr_t{ex.what()} };
	}
}
