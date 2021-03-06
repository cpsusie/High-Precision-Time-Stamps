#ifndef CJM_TESTS_HPP_
#define CJM_TESTS_HPP_
#include "helper.hpp"
#include <string>
namespace cjm::test
{
	template<typename TInvocable>
	void cjm_assert_nothrow(TInvocable invocable);

	template<typename TInvocable, typename TException>
	void cjm_assert_throws(TInvocable invocable);
	

	
	class cjm_test_fail final : public std::logic_error
	{
	public:
		cjm_test_fail(const cjm_test_fail& other) = delete;
		cjm_test_fail(cjm_test_fail&& other) noexcept = default;
		cjm_test_fail& operator=(const cjm_test_fail& other) = delete;
		cjm_test_fail& operator=(cjm_test_fail&& other) noexcept = default;

		cjm_test_fail(std::string&& m)
			: logic_error(m) {}
		~cjm_test_fail() override = default;

	};
	
	void cjm_assert(bool condition, fsv_t message);

	void cjm_deny(bool condition, fsv_t message);

	template<typename TInvocable>
	void cjm_assert_nothrow(TInvocable invocable)
	{
		try
		{
			invocable();
		}
		catch (...)
		{
			throw cjm_test_fail{ "The supplied function threw an exception."s };
		}
	}

	template<typename TInvocable, typename TException>
	void cjm_assert_throws(TInvocable invocable)
	{
		static_assert(std::is_assignable_v<std::exception*, TException*>, "Needs to be an exception.");
		try
		{

		}
		catch (const TException&)
		{
			return;
		}
		catch (...)
		{
			throw cjm_test_fail{ "Threw an exception of the wrong type." };
		}
		throw cjm_test_fail{ "Did not throw any exception" };
	}
	
}

namespace cjm::tests
{

	binary_operation produce_mult1_tc1_binary_op();
	binary_operation produce_div1_tc1_binary_op();
	binary_operation produce_mult1_tc1_rev_binary_op();
	binary_operation produce_div1_tc1_rev_binary_op();

	void run_mult_div_test_case_1();
	void run_tests();
	void test_serialize(int128_t serialize_me);
	void test_serialize_one_bin_op();
	void test_serialize_all_tc1_bin_op();
	void execute_test_case_one();
	void test_edge_case_comparisons();
}
#endif // CJM_TESTS_HPP_
