#include "tests.hpp"
#include <utility>
std::pair<double, cjm::int128_t> calculate_percent_diff(cjm::int128_t left, cjm::int128_t right)
{
	if (left == right) return std::make_pair<double, cjm::int128_t>(0, 0);

	if (left < 0 && right < 0)
	{
		if (left == std::numeric_limits<cjm::int128_t>::min())
		{
			left += 1; 
			right -= 1; //since left does not equal right
			if (right == std::numeric_limits<cjm::int128_t>::min()) //ok we switched places .... the difference is one and percent diff is 1 / min
			{
				return std::make_pair<double, cjm::int128_t>(static_cast<double>(1) / static_cast<double>(right == std::numeric_limits<cjm::int128_t>::min()), 1);
			}
		}
		if (right == std::numeric_limits<cjm::int128_t>::min())
		{
			right += 1;
			left -= 1; //since left does not equal right
			if (left == std::numeric_limits<cjm::int128_t>::min()) //ok we switched places .... the difference is one and percent diff is 1 / min
			{
				return std::make_pair<double, cjm::int128_t>(static_cast<double>(1) / static_cast<double>(right == std::numeric_limits<cjm::int128_t>::min()), 1);
			}			
		}
		//ok we know it's safe to make them both positive.
		left = -left;
		right = -right;		
	}

	auto throw_if_diff_too_big = [](cjm::int128_t bigger, cjm::int128_t smaller) -> cjm::int128_t
	{
		assert(bigger >= 0);
		if (smaller < 0)
		{
			auto bigger_us = static_cast<cjm::uint128_t>(bigger);
			auto smaller_us = static_cast<cjm::uint128_t>(smaller);
			auto sum = bigger_us + smaller_us;
			auto s_sum = static_cast<cjm::int128_t>(sum);
			if (s_sum <= bigger) throw std::domain_error{ "The difference would cause signed integer overflow." };
			return s_sum;
		}
		auto temp = bigger - smaller;
		if (temp == std::numeric_limits<cjm::int128_t>::min()) throw std::domain_error{ "Difference cannot be expressed as a positive signed int128." };
		return temp < 0 ? -temp : temp;
	};
	
	auto bigger = left > right ? left : right;
	auto smaller = left < right ? left : right;
	//std::cout << "bigger comparand: [" << std::dec << bigger << "]; smaller comparand: [" << std::dec << smaller << "].";
	const auto difference = throw_if_diff_too_big(bigger, smaller);

	const auto doubleSmallest = static_cast<double>(std::numeric_limits<cjm::int128_t>::min());
	if (difference == 0) return std::pair<double, cjm::int128_t>(0, 0);
	double percentDiff;
	
	if (bigger < 0)
	{
		if (smaller == std::numeric_limits<cjm::int128_t>::min())
		{
			percentDiff = static_cast<double>(difference) / doubleSmallest;
		}
		else
		{
			percentDiff = static_cast<double>(difference) / static_cast<double>(-smaller);
		}
	}
			
	assert(difference != 0 && bigger != 0);
	return std::make_pair<double, cjm::int128_t>(static_cast<double>(difference) / static_cast<double>(bigger), static_cast<cjm::int128_t>(difference < 0 ? -difference : difference));
}

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
	if (!condition) throw cjm_test_fail{fstr_t{message} };
}

void cjm::test::cjm_deny(bool condition, fsv_t message)
{
	if (condition) throw cjm_test_fail{fstr_t{message} };
}

cjm::binary_operation cjm::tests::produce_mult1_tc1_binary_op()
{
	constexpr int128_t ticks = -7'670'048'174'861'859'330;
	constexpr int128_t factor = 1'220'709;
	constexpr binary_op op = binary_op::multiply;
	return binary_operation{ op, ticks, factor, true };
}

cjm::binary_operation cjm::tests::produce_div1_tc1_binary_op()
{
	binary_operation multtc = produce_mult1_tc1_binary_op();
	multtc.calculate_result();
	int128_t dividend = multtc.result().value();
	constexpr int128_t divisor = 5'000'000;
	return binary_operation{ binary_op::divide, dividend, divisor, true };
}

cjm::binary_operation cjm::tests::produce_mult1_tc1_rev_binary_op()
{
	binary_operation to_result = produce_div1_tc1_binary_op();
	to_result.calculate_result();
	constexpr int128_t factor = 5'000'000;
	return binary_operation{ binary_op::multiply, to_result.result().value(), factor, true };
}

cjm::binary_operation cjm::tests::produce_div1_tc1_rev_binary_op()
{
	binary_operation prior_res = produce_mult1_tc1_rev_binary_op();
	prior_res.calculate_result();
	constexpr int128_t divisor = 1'220'709;
	return binary_operation{ binary_op::divide, prior_res.result().value(), divisor, true };
}

void cjm::tests::run_mult_div_test_case_1()
{
	using test::cjm_assert;
	constexpr int128_t ticks = -7'670'048'174'861'859'330;
	constexpr int128_t factor = 1'220'709;
	constexpr int128_t divisor = 5'000'000;
	cjm_assert(ticks > std::numeric_limits<std::int64_t>::min(), "original ticks does not fit in int64.");
	int128_t ts_ticks_to_sw_ticks = ticks * factor / divisor;
	cjm_assert(ts_ticks_to_sw_ticks > std::numeric_limits<std::int64_t>::min(), "resultant sw ticks does not fit in int64.");
	int128_t first_step_res = ticks * factor;
	int128_t second_step_res = first_step_res / divisor;
	std::cout << ticks << " * " << factor << " / " << divisor << " == " << ts_ticks_to_sw_ticks << newl;
	std::cout << "first step res: " << first_step_res << newl;
	std::cout << "second step res: " << second_step_res << newl;
	cjm_assert(second_step_res == ts_ticks_to_sw_ticks, "The result of doing the operations separately differs from the results done together."sv);
	binary_operation first_op = produce_mult1_tc1_binary_op();
	cjm_assert(first_op.left_operand() == ticks, "The left operand of the first op does not match up with the one used here."sv);
	cjm_assert(first_op.right_operand() == factor, "The right operand of the first op does not match up with the one used here."sv);
	cjm_assert(first_op.result().value() == first_step_res, "The mult done in the binary op has a different result from the one done here."sv);
	binary_operation second_op = produce_div1_tc1_binary_op();
	cjm_assert(second_op.left_operand() == first_step_res, "The left operand of the second step is not the result first step."sv);
	cjm_assert(second_op.right_operand() == divisor, "The right operand of the second step is not the same as divisor."sv);
	cjm_assert(second_op.result().value() == second_step_res, "The result of the second step done in binary op differs from one done here"sv);

	int128_t sw_ticks_to_ts_ticks = ts_ticks_to_sw_ticks * divisor / factor;
	int128_t rev_first_step_res = ts_ticks_to_sw_ticks * divisor;
	int128_t rev_second_step_res = rev_first_step_res / factor;
	std::cout << ts_ticks_to_sw_ticks << " * " << divisor << " / " << factor << " == " << sw_ticks_to_ts_ticks << newl;
	binary_operation first_rev_op = produce_mult1_tc1_rev_binary_op();
	cjm_assert(first_rev_op.left_operand() == ts_ticks_to_sw_ticks && first_rev_op.right_operand() == divisor && first_rev_op.op_code() == binary_op::multiply, 
		"The first reverse op does not match what we did here."sv);
	cjm_assert(first_rev_op.result().value() == rev_first_step_res, "The first step done in the binary op does not match what we did here."sv);
	binary_operation second_rev_op = produce_div1_tc1_rev_binary_op();
	cjm_assert(second_rev_op.left_operand() == first_rev_op.result().value() && second_rev_op.right_operand() == factor && second_rev_op.op_code() == binary_op::divide, 
		"the reversed second binary operation does not match what we did here."sv);
	cjm_assert(rev_second_step_res == sw_ticks_to_ts_ticks, "united operation does not match sequential."sv);
	cjm_assert(sw_ticks_to_ts_ticks == second_rev_op.result().value(), "Results of reverse binary operations does not match what we did here.");
	cjm_assert(sw_ticks_to_ts_ticks > std::numeric_limits<std::int64_t>::min(), "Result of conversion back to ts ticks does not fit in int64.");

	auto [percent_diff, diff] = calculate_percent_diff(sw_ticks_to_ts_ticks, ticks);
	std::cout << "Absolute value of difference between original and round tripped: [" << std::dec << diff << "]; Percentage difference: [" << percent_diff << "]." << newl;
	
	
}

void cjm::tests::execute_test_case_one()
{
	using cjm::test::cjm_assert;
	try
	{
		static constexpr uint128_t rt_difference = 1;

		constexpr auto close_enough = [](int128_t l, int128_t r) -> bool
		{
			if (l == r) return true;
			auto lu = static_cast<uint128_t>(l);
			auto ru = static_cast<uint128_t>(r);
			auto bigger = lu > ru ? lu : ru;
			auto smaller = lu < ru ? lu : ru;
			return bigger - smaller <= rt_difference;
		};
		
		std::array<binary_operation, 4> arr = { produce_mult1_tc1_binary_op(), produce_div1_tc1_binary_op(), produce_mult1_tc1_rev_binary_op(), produce_div1_tc1_rev_binary_op() };
		cjm_assert(std::all_of(arr.begin(), arr.end(), [](binary_operation& op) -> bool
			{
				op.calculate_result();
				return op.has_correct_result();
			}), "One or more of the operations has incorrect result"sv);

		auto final_result = arr[3].result().value();
		auto starting_value = arr[0].left_operand();
		std::cout << "starting value: \t\t\t[" << std::dec << starting_value << "]." << newl;
		std::cout << "round tripped final: \t\t\t[" << std::dec << final_result << "]." << newl;
		cjm_assert(close_enough(final_result, starting_value), "The starting and round trip values hare not close enough.");
	}
	catch (const test::cjm_test_fail&)
	{
		throw;
	}
	catch (const std::exception& ex)
	{
		throw test::cjm_test_fail{ fstr_t{ex.what()} };
	}
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
		
		test_name = "mult_div_test_case_1"sv;
		do_test(test_name, []() -> void
			{
				run_mult_div_test_case_1();
			});
		test_name = "test_case_one"sv;
		do_test(test_name, []() -> void
			{
				execute_test_case_one();
			});
		test_name = "test_serialize_one_bin_op"sv;
		do_test(test_name, []() -> void
			{
				test_serialize_one_bin_op();
			});
		test_name = "test_serialize_all_tc1_bin_op"sv;
		do_test(test_name, []() -> void
			{
				test_serialize_all_tc1_bin_op();
			});
		
	}
	catch (const test::cjm_test_fail&)
	{
		throw;
	}
	catch (const std::exception& ex)
	{
		fstr_stream_t stream;
		stream << "Test [" << test_name << "] FAILED with exception message: [" << ex.what() << "].";
		throw test::cjm_test_fail{ stream.str() };
	}
	catch (...)
	{
		throw test::cjm_test_fail{ "Test failed because a non-standard exception was thrown as the exception object."s };
	}
	std::cout << "All tests PASS!" << newl;
	
}

void cjm::tests::test_serialize(int128_t serialize_me)
{
	try
	{
		using test::cjm_assert;
		tstr_t text = serialize(serialize_me);
		int128_t round_tripped = deserialize(text);
		cjm_assert(round_tripped == serialize_me, "round tripped value does not equal original!");
		tstr_stream_t ss;
		serialize(ss, round_tripped);
		tstr_t stream_serialized = ss.str();
		cjm_assert(text == stream_serialized, "The stream serialization does not produce the same result as the string serialization.");
		
	}
	catch (const test::cjm_test_fail&)
	{
		throw;
	}
	catch (const std::exception& ex)
	{
		throw test::cjm_test_fail{ fstr_t{ex.what()} };
	}
}

void cjm::tests::test_serialize_one_bin_op()
{
	try
	{
		constexpr fsv_t file_name = "mul_tc1_first_bin_op.txt";
		
		binary_operation serialize_me = produce_mult1_tc1_binary_op();
		binary_operation_serdeser ser_util{};
		ser_util << serialize_me;
		std::basic_ofstream<tchar_t, std::char_traits<tchar_t>> output_stream;
		output_stream.exceptions(std::ios_base::badbit | std::ios_base::failbit);
		output_stream.open(file_name.data());
		output_stream << ser_util;
		output_stream.close();		
	}
	catch (const test::cjm_test_fail&)
	{
		throw;
	}
	catch (const std::exception& ex)
	{
		throw test::cjm_test_fail{ fstr_t{ex.what()} };
	}
	
}

void cjm::tests::test_serialize_all_tc1_bin_op()
{
	try
	{
		constexpr fsv_t file_name = "mul_tc1_all_bin_op.txt";
		auto vector = std::vector<binary_operation>{};
		vector.reserve(4);
		vector.emplace_back(produce_mult1_tc1_binary_op());
		vector.emplace_back(produce_div1_tc1_binary_op());
		vector.emplace_back(produce_mult1_tc1_rev_binary_op());
		vector.emplace_back(produce_div1_tc1_rev_binary_op());
		std::basic_ofstream<tchar_t, std::char_traits<tchar_t>> output_stream;
		output_stream.exceptions(std::ios_base::badbit | std::ios_base::failbit);
		output_stream.open(file_name.data());
		output_stream << vector;
		output_stream.close();
	}
	catch (const test::cjm_test_fail&)
	{
		throw;
	}
	catch (const std::exception& ex)
	{
		throw test::cjm_test_fail{ fstr_t{ex.what()} };
	}
}

void cjm::tests::test_edge_case_comparisons()
{
	try
	{
		test::cjm_assert(!edge_tests_comparison_v.empty(), "edge test comparisons should not be empty.");
		test::cjm_assert(std::all_of(edge_tests_comparison_v.cbegin(), edge_tests_comparison_v.cend(), [](const binary_operation& op) -> bool
			{
				return op.has_correct_result();
			}), "One or more operations lack a result or lack the correct result."sv);
	}
	catch (const test::cjm_test_fail&)
	{
		throw;
	}
	catch (const std::exception& ex)
	{
		throw test::cjm_test_fail{ fstr_t{ex.what()} };
	}
}
