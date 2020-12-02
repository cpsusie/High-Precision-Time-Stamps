#include "tests.hpp"

template<typename Invocable>
void do_test(cjm::fsv_t name, Invocable do_me)
{
	try
	{
		std::cout << "BEGIN " << name << " TEST: " << cjm::newl;
		do_me();
		std::cout << "END " << name << " TEST: " << cjm::newl;
	}
	catch (const cjm::test::cjm_test_fail& ex)
	{
		std::cerr << "Test [" << name << "] FAILED: [" << ex.what() << "]." << cjm::newl;
		throw;
	}
	
}

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
		do_test(test_name, []() -> void
		{
				std::int64_t high = 0xc0de'd00d'fea2'b00b;
				std::uint64_t low = 0xc0de'd00d'fea2'b00b;
				int128_t test = absl::MakeInt128(high, low);
				test_serialize(test);
		});
		
		test_name = "test_edge_ops"sv;
		do_test(test_name, []() -> void
		{
				test_edge_case_comparisons();
		});
		
	}
	catch (const cjm::test::cjm_test_fail&)
	{
		throw;
	}
	catch (const std::exception& ex)
	{
		cjm::fstr_stream_t stream;
		stream << "Test [" << test_name << "] FAILED with exception message: [" << ex.what() << "].";
		throw cjm::test::cjm_test_fail{ stream.str() };
	}
	catch (...)
	{
		throw cjm::test::cjm_test_fail{ "Test failed because a non-standard exception was thrown as the exception object."s };
	}
	std::cout << "All tests PASS!" << newl;
	
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

void cjm::tests::test_edge_case_comparisons()
{
	try
	{
		test::cjm_assert(!cjm::edge_tests_comparison_v.empty(), "edge test comparisons should not be empty.");
		test::cjm_assert(std::all_of(edge_tests_comparison_v.cbegin(), edge_tests_comparison_v.cend(), [](const binary_operation& op) -> bool
			{
				return op.has_correct_result();
			}), "One or more operations lack a result or lack the correct result."sv);
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
