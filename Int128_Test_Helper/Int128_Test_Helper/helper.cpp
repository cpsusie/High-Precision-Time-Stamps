#include "helper.hpp"
#include <vector>
#include <cassert>
#include <algorithm>
#include <cstring>

std::unique_ptr<cjm::cjm_helper_rgen> s_ptr = cjm::cjm_helper_rgen::make_rgen();  // NOLINT(clang-diagnostic-exit-time-destructors) YES ... I Know

std::pair<bool, int> parse_int(cjm::fsv_t str) noexcept;

std::uint64_t parse_u(cjm::tsv_t parse);

std::int64_t parse_s(cjm::tsv_t parse);

cjm::fstr_t to_fstr_t(cjm::tsv_t convert);


cjm::tstr_t cjm::to_tstr_t(fsv_t convert)
{
	tstr_t ret;
	if (!convert.empty())
	{
		ret.reserve(convert.size());
		std::transform(convert.cbegin(), convert.cend(), std::back_inserter(ret), [](fchar_t c) -> cjm::tchar_t
		{
				return static_cast<fchar_t>(c);
		});
	}
	return ret;
}

cjm::tstr_t cjm::serialize(int128_t value)
{
	fstr_stream_t stream;
	std::int64_t high = absl::Int128High64(value);
	std::uint64_t low = absl::Int128Low64(value);
	stream << std::hex << std::setw(sizeof(int64_t) * 2) << std::setfill('0')
		<< low << '\t'
		<< std::hex << std::setw(sizeof(int64_t) * 2) << std::setfill('0')
		<< high << '\t';
	auto temp = stream.str();
	return to_tstr_t(temp);
}

void cjm::serialize(tostrm_t& ostr, int128_t value)
{
	std::int64_t high = absl::Int128High64(value);
	std::uint64_t low = absl::Int128Low64(value);
	ostr << std::hex << std::setw(sizeof(int64_t) * 2) << std::setfill(u'0')
		<< low << u'\t'
		<< std::hex << std::setw(sizeof(int64_t) * 2) << std::setfill(u'0')
		<< high << u'\t';
}

bool cjm::operator==(binary_operation_serdeser lhs, binary_operation_serdeser rhs) noexcept
{
	return lhs.m_op_view == rhs.m_op_view;
}

bool cjm::operator!=(binary_operation_serdeser lhs, binary_operation_serdeser rhs) noexcept
{
	return !(lhs == rhs);
}

cjm::binary_operation_serdeser& cjm::operator<<(binary_operation_serdeser& bosds, const binary_operation& bin_op)
{
	bosds.m_op_view = &bin_op;
	return bosds;
}

cjm::tostrm_t& cjm::operator<<(tostrm_t& ostr, const binary_operation_serdeser& other)
{
	constexpr auto field_delim = binary_operation_serdeser::item_field_delimiter;
	if (other.has_value())
	{
		auto x = *other;
		ostr << text(x.op_code()).value() << field_delim;
		serialize(ostr, x.left_operand());
		ostr << field_delim;
		serialize(ostr, x.right_operand());
		ostr << field_delim;
		if (!x.has_correct_result())
		{
			x.calculate_result();
			assert(x.has_correct_result());			
		}
		serialize(ostr, x.result().value());
		ostr << field_delim;		
	}
	return ostr;
}

cjm::int128_t cjm::deserialize(tsv_t deser_me)
{
	auto split = cjm::split(deser_me, u'\t');
	if (split.empty())
	{
		throw std::invalid_argument{ "string does not contain any text." };
	}
	tsv_t low;
	tsv_t high;
	size_t found = 0;
	for (auto sv : split)
	{
		if (!sv.empty())
		{
			switch (found)
			{
			case 0:
				low = sv;
				++found;
				break;
			case 1:
				high = sv;
				++found;
				break;
			default:
				throw std::invalid_argument{ "Too much data in string." };
			}			
		}
		if (found >= 2)
			break;
	}
	if (found < 2)
	{
		throw std::invalid_argument{ "Not enough data in string." };
	}
	try
	{
		std::uint64_t low_v = parse_u(low);
		std::int64_t high_v = parse_s(high);
		return absl::MakeInt128(high_v, low_v);
	}
	catch (const std::exception& ex)
	{
		throw std::invalid_argument{ "Unable to parse supplied text as int128: "s + fstr_t{ex.what()} };
	}
}

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
cjm::binary_operation::binary_operation() noexcept : m_op{ binary_op::left_shift }, m_lhs{}, m_rhs{} {}

cjm::binary_operation::binary_operation(binary_op op, int128_t first_operand, int128_t second_operand,
	bool calculate_now) : m_op{op}, m_lhs{first_operand}, m_rhs{ second_operand}, m_result{}
{
	size_t op_code = static_cast<size_t>(op);
	if (op_code >= op_name_lookup.size())
		throw std::invalid_argument{ "The op code is not recognized." };
	if (calculate_now)
	{
		this->calculate_result();
	}
}

cjm::binary_operation::binary_operation(binary_op op, int128_t first_operand, int128_t second_operand)
	: binary_operation{op, first_operand, second_operand, false}{}

cjm::binary_operation::
binary_operation(binary_op op, int128_t first_operand, int128_t second_operand, int128_t result):
	m_op{ op }, m_lhs{ first_operand }, m_rhs{ second_operand },  m_result{result}
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
	case binary_op::left_shift:
		ret = lhs << static_cast<int>(rhs);
		break;
	case binary_op::right_shift:
		ret = lhs >> static_cast<int>(rhs);
		break;
	case binary_op::bw_and:
		ret = lhs & rhs;
		break;
	case binary_op::bw_or:
		ret = lhs | rhs;
		break;
	case binary_op::bw_xor:
		ret = lhs ^ rhs;
		break;
	case binary_op::divide:
		ret = lhs / rhs;
		break;
	case binary_op::modulus:
		ret = lhs % rhs;
		break;
	case binary_op::add:
		ret = lhs + rhs;
		break;
	case binary_op::subtract:
		ret = lhs - rhs;
		break;
	case binary_op::multiply:
		ret = lhs * rhs;
		break;
	case binary_op::compare: 
		if (lhs == rhs)
		{
			ret = 0;
		}
		else
		{
			ret = lhs > rhs ? 1 : -1;
		}
		break;
	
	}
	return ret;
}

bool cjm::binary_operation_serdeser::has_value() const noexcept
{
	return m_op_view != nullptr;
}

cjm::binary_operation cjm::binary_operation_serdeser::value() const
{
	if (has_value())
	{
		return *m_op_view;
	}
	throw bad_value_access{};
}

cjm::binary_operation cjm::binary_operation_serdeser::value_or_default() const noexcept
{
	if (has_value())
	{
		return *m_op_view;
	}
	return cjm::binary_operation{};
}

const cjm::binary_operation& cjm::binary_operation_serdeser::operator*() const noexcept
{
	return *m_op_view;
}

const cjm::binary_operation* cjm::binary_operation_serdeser::operator->() const noexcept
{
	return m_op_view;
}

cjm::binary_operation_serdeser::operator bool() const noexcept
{
	return m_op_view != nullptr;
}

bool cjm::binary_operation_serdeser::operator!() const noexcept
{
	return m_op_view == nullptr;
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
	case binary_op::left_shift: 		
	case binary_op::right_shift:
		l_op = m_operand_distrib(m_twister);
		r_op = m_shift_distrib(m_twister);
		break;
	case binary_op::compare:
	case binary_op::add:
	case binary_op::subtract:
	case binary_op::bw_and:
	case binary_op::bw_or:
	case binary_op::bw_xor:
		l_op = make_full_range();
		r_op = make_full_range();
		break;
	case binary_op::modulus:
	case binary_op::divide:
		//std::uint64_t high = m_operand_distrib(m_twister);
		//std::uint64_t low = m_operand_distrib(m_twister);
		//uint128_t temp = high;
		//temp <<= 64;
		//temp |= low;
		l_op = make_full_range();
		r_op = m_operand_distrib(m_twister);		
		break;
	case binary_op::multiply:
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
                                           ).count()) }, m_twister{ m_seed }, m_op_distrib{ std::uniform_int_distribution<int>(std::int64_t{0}, static_cast<std::int64_t>(op_name_lookup.size()) - std::int64_t{1}) },
                                           m_shift_distrib{ std::uniform_int_distribution<int>(0, 127)},
                                           m_operand_distrib{ std::uniform_int_distribution<std::int64_t>(std::numeric_limits<std::int64_t>::min() + std::int64_t{1},
	                                           std::numeric_limits<std::int64_t>::max()) }
{
	std::cout << "Hi mom!" << newl;
}

int cjm::execute(int argc, char* argv[])
{
	try
	{
		cmd_args files = extract_arr(argc, argv);
		assert(files.good() && !files.first_file().empty() && files.op_count() > 0);
		std::cout << "First file name: [" << files.first_file() << "]." << newl;
		std::cout << "Second file name: [" << files.second_file() << "]." << newl;
		std::cout << "Number of ops: [" << files.op_count() << "]." << newl;

		constexpr fsv_t comp_edge_batter = "Comparison Edge Case Test Battery";
		constexpr fsv_t comp_edge_case_file = "comp_edge_ops.txt"sv;


		fsv_t this_battery = comp_edge_batter;
		fsv_t this_file = comp_edge_case_file;
		const std::vector<binary_operation>* this_vector = &edge_tests_comparison_v;
		
		serialize_binary_ops(this_battery, this_file, *this_vector);
		//std::cout << "Going to write comparison edge case"
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
		char* first_argument;
		char* second_argument;
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
		int pos;
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

void cjm::serialize_binary_ops(fsv_t test_battery_name, fsv_t file_name, const std::vector<binary_operation>& ops)
{
	if (file_name.empty())
	{
		throw std::invalid_argument{ "File name supplied cannot be empty." };
	}
	if (ops.empty())
	{
		throw std::invalid_argument{ "Ops vector cannot be empty." };
	}
	try
	{
		std::cout << "Saving " << test_battery_name << " to file [" << file_name << "]... ";
		auto stream = tofstrm_t{};
		stream.exceptions(std::ios::badbit | std::ios::failbit);
		stream.open(file_name.data());		
		stream << ops;
		stream.close();		
	}
	catch (const std::exception& ex)
	{
		fstr_stream_t message;
		message << "Unable to save "sv << test_battery_name << " to file "sv << file_name
			<< " because of exception: ["sv << ex.what() << "]."sv;
		throw std::runtime_error{ message.str() };
	}
	std::cout << " successfully saved battery " << test_battery_name << " to file: [" << file_name << "]." << newl;
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

std::uint64_t parse_u(cjm::tsv_t parse)
{
	std::uint64_t ret = 0;
	cjm::fstr_t converted = to_fstr_t(parse);
	cjm::fstr_stream_t stream;
	stream.exceptions(std::ios::failbit | std::ios::badbit);
	stream << std::hex << converted;
	stream >> ret;
	return ret;
}

std::int64_t parse_s(cjm::tsv_t parse)
{
	std::int64_t ret = 0;
	std::uint64_t temp = 0;
	cjm::fstr_t converted = to_fstr_t(parse);
	cjm::fstr_stream_t stream;
	stream.exceptions(std::ios::failbit | std::ios::badbit);
	stream << std::hex << converted;
	stream >> temp;
	std::memcpy(&ret, &temp, sizeof(uint64_t));
	return ret;
}

cjm::fstr_t to_fstr_t(cjm::tsv_t convert)
{
	constexpr auto f_size = sizeof(cjm::fchar_t);
	constexpr auto t_size = sizeof(cjm::tchar_t);
	constexpr bool is_fsigned = std::numeric_limits<cjm::fchar_t>::is_signed;
	constexpr bool is_tsigned = std::numeric_limits<cjm::tchar_t>::is_signed;
	static_assert(f_size <= t_size, "fchar must be less than tchar");

	if constexpr (is_fsigned == is_tsigned)
	{
		constexpr cjm::tchar_t max_fchar = static_cast<cjm::tchar_t>(std::numeric_limits<cjm::fchar_t>::max());
		constexpr cjm::tchar_t min_fchar = static_cast<cjm::tchar_t>(std::numeric_limits<cjm::fchar_t>::min());
		cjm::fstr_t ret;
		if (!convert.empty())
		{
			ret.reserve(convert.size());
			std::transform(convert.cbegin(), convert.cend(), std::back_inserter(ret), [=](cjm::tchar_t c) -> cjm::fchar_t
				{
					if (c > max_fchar || c < min_fchar) throw std::invalid_argument{ "character out of range for conversion." };  // NOLINT(misc-redundant-expression) -- only redundant with current aliases
					return static_cast<cjm::fchar_t>(c);
				});
		}
		return ret;
	}
	else if constexpr (is_fsigned)
	{
		using ufchar_t = std::make_unsigned_t<cjm::fchar_t>;

		constexpr auto umax = static_cast<ufchar_t>(std::numeric_limits<cjm::fchar_t>::max());
		constexpr auto umin = static_cast<ufchar_t>(std::numeric_limits<cjm::fchar_t>::min());
		constexpr auto max_fchar = static_cast<std::int32_t>(umax);
		constexpr auto min_fchar = -static_cast<std::int32_t>(umin);
		cjm::fstr_t ret;
		if (!convert.empty())
		{
			ret.reserve(convert.size());
			std::transform(convert.cbegin(), convert.cend(), std::back_inserter(ret), [=](cjm::tchar_t c) -> cjm::fchar_t
				{
					if (static_cast<std::int32_t>(c) > max_fchar || static_cast<std::int32_t>(c) < min_fchar) throw std::invalid_argument{ "character out of range for conversion." };  // NOLINT(misc-redundant-expression) -- only redundant with current aliases
					return static_cast<cjm::fchar_t>(c);
				});
		}
		return ret;
	}
	else
	{
		constexpr auto max_fchar = static_cast<std::uint32_t>(static_cast<cjm::tchar_t>(std::numeric_limits<cjm::fchar_t>::max()));
		constexpr auto min_fchar = static_cast<std::uint32_t>(static_cast<cjm::tchar_t>(std::numeric_limits<cjm::fchar_t>::min()));
		cjm::fstr_t ret;
		if (!convert.empty())
		{
			ret.reserve(convert.size());
			std::transform(convert.cbegin(), convert.cend(), std::back_inserter(ret), [=](cjm::tchar_t c) -> cjm::fchar_t
				{
					if (static_cast<uint32_t>(c) > max_fchar || static_cast<uint32_t>(c) < min_fchar) throw std::invalid_argument{ "character out of range for conversion." };  // NOLINT(misc-redundant-expression) -- only redundant with current aliases
					return static_cast<cjm::fchar_t>(c);
				});
		}
		return ret;
	}

	
	
	
}
