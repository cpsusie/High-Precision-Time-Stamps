#include "helper.hpp"
#include <vector>
#include <cassert>

std::unique_ptr<cjm::cjm_helper_rgen> s_ptr = cjm::cjm_helper_rgen::make_rgen();  // NOLINT(clang-diagnostic-exit-time-destructors) YES ... I Know

std::pair<bool, int> parse_int(cjm::fsv_t str) noexcept;

std::vector<cjm::binary_operation> cjm::create_random_ops(size_t count)
{
	assert(s_ptr != nullptr);
	auto ret = std::vector<cjm::binary_operation>();
	ret.reserve(count);
	while (ret.size() < count)
	{
		ret.emplace_back(s_ptr->random_operation());
	}
	return ret;
}
std::vector<cjm::binary_operation> cjm::create_random_ops(size_t count, binary_op op_code)
{
	assert(s_ptr != nullptr);
	auto ret = std::vector<cjm::binary_operation>();
	ret.reserve(count);
	while (ret.size() < count)
	{
		ret.emplace_back(s_ptr->random_operation(op_code));
	}
	return ret;
}
cjm::binary_operation::binary_operation() noexcept : m_op{ binary_op::LeftShift }, m_rhs{}, m_lhs{} {}

cjm::binary_operation::binary_operation(binary_op op, int128_t first_operand, int128_t second_operand):
	m_op{op}, m_rhs{first_operand}, m_lhs{second_operand}, m_result{}
{
	size_t op_code = static_cast<size_t>(op);
	if (op_code >= op_name_lookup.size())
		throw std::invalid_argument{"The op code is not recognized."};
}

cjm::binary_operation::
binary_operation(binary_op op, int128_t first_operand, int128_t second_operand, int128_t result):
	m_op{op}, m_rhs{first_operand}, m_lhs{second_operand}, m_result{result}
{
	size_t op_code = static_cast<size_t>(op);
	if (op_code >= op_name_lookup.size())
		throw std::invalid_argument{"The op code is not recognized."};
}

bool cjm::binary_operation::do_calculate_result()
{
	int128_t result = perform_calculate_result(m_lhs, m_rhs, m_op);
	const bool changed_value = (!m_result.has_value() || *m_result != result);
	m_result = result;
	return changed_value;
}

cjm::int128_t cjm::binary_operation::perform_calculate_result(int128_t lhs, int128_t rhs, binary_op op) noexcept
{
	assert(static_cast<size_t>(op) < op_name_lookup.size());
	int128_t ret = 0;
	switch (op)
	{
	case LeftShift:
		ret = lhs << static_cast<int>(rhs);
		break;
	case RightShift:
		ret = lhs >> static_cast<int>(rhs);
		break;
	case And:
		ret = lhs & rhs;
		break;
	case Or:
		ret = lhs | rhs;
		break;
	case Xor:
		ret = lhs ^ rhs;
		break;
	case Divide:
		ret = lhs / rhs;
		break;
	case Modulus:
		ret = lhs % rhs;
		break;
	case Add:
		ret = lhs + rhs;
		break;
	case Subtract:
		ret = lhs - rhs;
		break;
	case Multiply:
		ret = lhs * rhs;
		break;
	}
	return ret;
}

cjm::fsv_t cjm::cmd_args::first_file() const noexcept
{
	return m_arr[0];
}

cjm::fsv_t cjm::cmd_args::second_file() const noexcept
{
	return m_arr[1];
}

int cjm::cmd_args::op_count() const noexcept
{
	return m_num_ops;
}

bool cjm::cmd_args::good() const noexcept
{
	return !first_file().empty() && op_count() > 0;
}

cjm::cmd_args::cmd_args(const fstr_arr_t& arr, int num_ops): m_num_ops{num_ops}
{
	if (num_ops < 1)
		throw std::domain_error{"At least one operation must be specified."};
	if (arr[0].empty())
		throw std::domain_error{"At least one file name must be specified."};
	m_arr = arr;
}

cjm::cmd_args::cmd_args(cmd_args&& other) noexcept: m_num_ops(other.op_count()), m_arr{}
{
	std::swap(m_arr[0], other.m_arr[0]);
	std::swap(m_arr[1], other.m_arr[1]);
}

cjm::cmd_args& cjm::cmd_args::operator=(cmd_args&& other) noexcept
{
	if (this != &other)
	{
		m_num_ops = other.m_num_ops;
		std::swap(m_arr[0], other.m_arr[0]);
		std::swap(m_arr[1], other.m_arr[1]);
	}
	return *this;
}

std::unique_ptr<cjm::cjm_helper_rgen> cjm::cjm_helper_rgen::make_rgen()
{
	auto* tmp = new cjm_helper_rgen();
	return std::unique_ptr<cjm_helper_rgen>{tmp};
}

cjm::binary_op cjm::cjm_helper_rgen::random_binary_op()
{
	const auto value = m_op_distrib(m_twister);
	return static_cast<binary_op>(value);
}

cjm::int128_t cjm::cjm_helper_rgen::random_shift_arg()
{
	const auto value = m_shift_distrib(m_twister);
	assert(value > -1 && value < 128);
	return value;
}

cjm::int128_t cjm::cjm_helper_rgen::random_operand_arg()
{
	const auto value = m_operand_distrib(m_twister);
	assert(value > std::numeric_limits<std::int64_t>::min() && value <= std::numeric_limits<std::int64_t>::max());
	return value;
}

cjm::binary_operation cjm::cjm_helper_rgen::random_operation(binary_op op)
{
	int128_t l_op;
	int128_t r_op;

	const auto make_full_range = [&]() -> int128_t
	{
		std::uint64_t high = m_operand_distrib(m_twister);
		std::uint64_t low = m_operand_distrib(m_twister);
		uint128_t temp = high;
		temp <<= 64;
		temp |= low;
		return static_cast<int128_t>(temp);
	};
	
	switch (op)
	{
	case LeftShift: 		
	case RightShift:
		l_op = m_operand_distrib(m_twister);
		r_op = m_shift_distrib(m_twister);
		break;		
	case Add:
	case Subtract:
	case And: 
	case Or: 
	case Xor: 
		l_op = make_full_range();
		r_op = make_full_range();
		break;
	case Modulus:
	case Divide:
		//std::uint64_t high = m_operand_distrib(m_twister);
		//std::uint64_t low = m_operand_distrib(m_twister);
		//uint128_t temp = high;
		//temp <<= 64;
		//temp |= low;
		l_op = make_full_range();
		r_op = m_operand_distrib(m_twister);		
		break;
	case Multiply:
		l_op = m_operand_distrib(m_twister);
		r_op = m_operand_distrib(m_twister);
		break;
	default:  // NOLINT(clang-diagnostic-covered-switch-default)
		l_op = 0;
		r_op = 0;
		break;
	}

	return binary_operation{ op, l_op, r_op };
}

cjm::binary_operation cjm::cjm_helper_rgen::random_operation()
{
	auto op = random_binary_op();
	return random_operation(op);
}


cjm::cjm_helper_rgen::cjm_helper_rgen() :  m_seed{ static_cast<std::mt19937_64::result_type>(std::chrono::duration_cast<std::chrono::microseconds>(
	                                           std::chrono::system_clock::now().time_since_epoch()
                                           ).count()) }, m_twister{ m_seed }, m_op_distrib{ std::uniform_int_distribution<int>(std::int64_t{0}, op_name_lookup.size() - std::int64_t{1}) },
                                           m_shift_distrib{ std::uniform_int_distribution<int>(0, 127)},
                                           m_operand_distrib{ std::uniform_int_distribution<std::int64_t>(std::numeric_limits<std::int64_t>::min() + std::int64_t{1},
	                                           std::numeric_limits<std::int64_t>::max()) } { }

int cjm::execute(int argc, char* argv[])
{
	try
	{
		cmd_args files = extract_arr(argc, argv);
		assert(files.good() && !files.first_file().empty() && files.op_count() > 0);
		std::cout << "First file name: [" << files.first_file() << "]." << newl;
		std::cout << "Second file name: [" << files.second_file() << "]." << newl;
		std::cout << "Number of ops: [" << files.op_count() << "]." << newl;
	}
	catch (const std::domain_error& ex)
	{
		std::cerr << "Error: [" << ex.what()  << "]." << newl;
		return -1;
	}
	return 0;
}

cjm::cmd_args cjm::extract_arr(int argc, char* argv[])
{
	fstr_arr_t arr;
	if (argc > 0)
	{
		char* first_argument = nullptr;
		char* second_argument = nullptr;
		char* third_argument = nullptr;
		switch (argc)
		{
		default:
		case 4:
			third_argument = argv[3];
			second_argument = argv[2];
			first_argument = argv[1];
			break;
		case 3:
			second_argument = argv[2];
			first_argument = argv[1];
			break;
		case 1:
		case 2:
			throw std::domain_error{ "There must be a file name and integer in the command line arguments." };			
		}

		fsv_t first_file_name;
		fsv_t second_file_name;
		fstr_t first_arg = first_argument ? fstr_t{ first_argument } : fstr_t{};
		fstr_t second_arg = second_argument ? fstr_t{ second_argument } : fstr_t{};
		fstr_t third_arg = third_argument ? fstr_t{ third_argument } : fstr_t{};
		auto first_is_number = parse_int(first_arg);
		auto second_is_number = parse_int(second_arg);
		auto third_is_number = parse_int(third_arg);
		std::vector<int> which_are_ints;
		if (first_is_number.first)
			which_are_ints.push_back(1);
		if (second_is_number.first)
			which_are_ints.push_back(2);
		if (third_is_number.first)
			which_are_ints.push_back(3);
		int int_val;
		int pos = -1;
		switch (which_are_ints.size())
		{
		case 0:
			throw std::domain_error{ "No integers were supplied in the command line arguments." };
		case 1:
			pos = which_are_ints[0];
			assert(pos > -1 && pos < 4);
			switch (pos)
			{
			
			case 1:
				int_val = first_is_number.second;
				break;
			case 2:
				int_val = second_is_number.second;
				break;
			default:
			case 3:
				int_val = third_is_number.second;
				break;
			}
			if (int_val <= 0) 
				throw std::domain_error{ "Number of operations specified must be positive." };
			break;
		default:	
		case 2:
			throw std::domain_error{ "Only one command line argument should contain the number of operations." };			
		}
		assert(pos > 0 && pos < 4);
		switch (pos)
		{
		case 1:
			first_file_name = second_arg.empty() ? third_arg : second_arg;
			second_file_name = first_file_name.empty() ? fsv_t{} : third_arg;
			break;
		case 2:
			first_file_name = first_arg.empty() ? third_arg : first_arg;
			second_file_name = first_file_name.empty() ? fsv_t{} : third_arg;
			break;
		default:	
		case 3:
			first_file_name = first_arg.empty() ? second_arg : first_arg;
			second_file_name = first_file_name.empty() ? fsv_t{} : second_arg;
			break;
		}
		arr[0] = first_file_name;
		arr[1] = second_file_name;
		return cmd_args{ arr, int_val };
	}
	throw std::domain_error{ "Two arguments needed: file name and positive integer.  Third file name optional." };
}

std::pair<bool, int> parse_int(cjm::fsv_t str) noexcept
{
	try
	{
		std::stringstream reader;
		reader.exceptions(std::ios::failbit);
		int num_ops = 0;
		reader << str;
		reader >> num_ops;
		return std::make_pair(true, num_ops);
	}
	catch (const std::exception&)
	{
		return std::make_pair(false, 0);
	}	
}
