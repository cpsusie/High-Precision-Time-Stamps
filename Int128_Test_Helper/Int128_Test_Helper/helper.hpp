#ifndef CJM_HELPER_HPP_
#define CJM_HELPER_HPP_
#include <string_view>
#include <string>
#include <limits>
#include <fstream>
#include <iostream>
#include <iomanip>
#include <exception>
#include <stdexcept>
#include <limits>
#include <type_traits>
#include <sstream>
#include <memory>
#include <array>
#include <optional>
#include <functional>
#include <absl/hash/hash.h>
#include <absl/numeric/int128.h>
#include <boost/functional/hash.hpp>
#include <random>
#include <chrono>
#include <mutex>
#include <cstdint>
#include <vector>
#include <utility>
namespace cjm
{
	using namespace std::string_literals;
	using namespace std::string_view_literals;
	constexpr auto newl = '\n';
	constexpr auto w_newl = u'\n';

	using uint128_t = absl::uint128;
	using int128_t = absl::int128;
	using fchar_t = char;
	using tchar_t = char16_t;
	using fstr_t = std::basic_string<fchar_t>;
	using fstr_stream_t = std::basic_stringstream<fchar_t>;
	using tstr_t = std::basic_string<tchar_t>;
	using fstr_arr_t = std::array<fstr_t, 2>;
	using fsv_t = std::basic_string_view<fchar_t>;
	using tsv_t = std::basic_string_view<tchar_t>;
	using tstr_stream_t = std::basic_stringstream<tchar_t>;
	using tostrm_t = std::basic_ostream<tchar_t>;
	using tistrm_t = std::basic_istream<tchar_t>;
	
	
	
	constexpr size_t binary_op_count = 11;
	enum class binary_op : unsigned int
	{
		left_shift = 0,
		right_shift,
		bw_and,
		bw_or,
		bw_xor,
		
		divide,
		modulus,
		add,
		subtract,
		multiply,

		compare		
	};


	
	template<typename Char, typename CharTraits = std::char_traits<Char>>
	std::vector<std::basic_string_view<Char, CharTraits>>
		split(std::basic_string_view<Char, CharTraits> split_me, Char split_on);
	
	class bad_value_access;
	struct binary_operation;
	struct binary_operation_serdeser;
	class cjm_helper_rgen;
	struct cmd_args;
	tstr_t to_tstr_t(fsv_t convert);
	tstr_t serialize(int128_t value);
	void serialize(tostrm_t& ostr, int128_t value);

	template<typename TSerDeser = binary_operation_serdeser>
	tostrm_t& operator<<(tostrm_t& ost, const std::vector<binary_operation>& col);
	
	
	bool operator==(binary_operation_serdeser lhs, binary_operation_serdeser rhs) noexcept;
	bool operator!=(binary_operation_serdeser lhs, binary_operation_serdeser rhs) noexcept;
	binary_operation_serdeser& operator
		<<(binary_operation_serdeser& bosds, const binary_operation& bin_op);
	tostrm_t& operator<<(tostrm_t& ostr, const binary_operation_serdeser& other);
	int128_t deserialize(tsv_t deser_me);
	std::vector<binary_operation> create_random_ops(size_t count);
	std::vector<binary_operation> create_random_ops(size_t count, binary_op op_code);
	int execute(int argc, char* argv[]);
	cmd_args extract_arr(int argc, char* argv[]);
	constexpr std::optional<tsv_t> text(binary_op op) noexcept;
	constexpr std::optional<binary_op> parse_op(tsv_t parse_me) noexcept;

	static std::vector<binary_operation> init_edge_comparisons();
	inline const std::vector<binary_operation> edge_tests_comparison_v = init_edge_comparisons();

	
	constexpr std::array<tsv_t, binary_op_count> op_name_lookup =
		std::array<tsv_t, binary_op_count>{
		u"LeftShift"sv, u"RightShift"sv,
			u"And"sv, u"Or"sv,
			u"Xor"sv, u"Divide"sv,
			u"Modulus"sv, u"Add"sv,
			u"Subtract"sv, u"Multiply"sv,
			u"Compare"sv};
	

	struct binary_operation
	{
		friend std::size_t hash_value(const binary_operation& obj)
		{
			
			std::size_t seed = 0x1FBB0493;
			boost::hash_combine(seed, static_cast<size_t>(obj.m_op));
			boost::hash_combine(seed, absl::Hash<int128_t>{}(obj.m_lhs));
			boost::hash_combine(seed, absl::Hash<int128_t>{}(obj.m_rhs));
			return seed;
		}

		friend bool operator<(const binary_operation& lhs, const binary_operation& rhs)
		{
			if (lhs.m_op < rhs.m_op)
				return true;
			if (rhs.m_op < lhs.m_op)
				return false;
			if (lhs.m_rhs < rhs.m_rhs)
				return true;
			if (rhs.m_rhs < lhs.m_rhs)
				return false;
			return lhs.m_lhs < rhs.m_lhs;
		}

		friend bool operator<=(const binary_operation& lhs, const binary_operation& rhs) { return !(rhs < lhs); }

		friend bool operator>(const binary_operation& lhs, const binary_operation& rhs) { return rhs < lhs; }

		friend bool operator>=(const binary_operation& lhs, const binary_operation& rhs) { return !(lhs < rhs); }

		friend bool operator==(const binary_operation& lhs, const binary_operation& rhs)
		{
			return lhs.m_op == rhs.m_op
				&& lhs.m_rhs == rhs.m_rhs
				&& lhs.m_lhs == rhs.m_lhs;
		}

		friend bool operator!=(const binary_operation& lhs, const binary_operation& rhs) { return !(lhs == rhs); }

		[[nodiscard]] binary_op op_code() const noexcept { return m_op;	}
		[[nodiscard]] int128_t left_operand() const noexcept{ return m_lhs; }
		[[nodiscard]] int128_t right_operand() const noexcept { return m_rhs; }
		[[nodiscard]] std::optional<int128_t> result() const noexcept { return m_result; }
		[[nodiscard]] bool has_result() const noexcept{ return m_result.has_value(); }
		[[nodiscard]] bool has_correct_result() const
		{
			return m_result.has_value() && binary_operation::perform_calculate_result(m_lhs, m_rhs, m_op) == m_result.value();
		}
		
		binary_operation() noexcept;
		binary_operation(binary_op op, int128_t first_operand, int128_t second_operand, bool calculate_now);
		binary_operation(binary_op op, int128_t first_operand, int128_t second_operand);
		binary_operation(binary_op op, int128_t first_operand, int128_t second_operand, int128_t result);
		binary_operation(const binary_operation& other) noexcept= default;
		binary_operation(binary_operation&& other) noexcept = default;
		binary_operation& operator=(const binary_operation& other) noexcept = default;
		binary_operation& operator=(binary_operation&& other) noexcept = default;
		~binary_operation() =default;

		void calculate_result() { do_calculate_result(); }
		
	private:

		bool do_calculate_result();
		static int128_t perform_calculate_result(int128_t lhs, int128_t rhs, binary_op op) noexcept;

		binary_op m_op;
		int128_t m_lhs;
		int128_t m_rhs;
		std::optional<int128_t> m_result;
		
	};
	
	struct binary_operation_serdeser
	{
		static constexpr tsv_t item_delimiter = u"\n"sv;
		static constexpr tchar_t item_field_delimiter = u';';
		friend struct std::hash<binary_operation_serdeser>;
		friend bool operator==(binary_operation_serdeser lhs, binary_operation_serdeser rhs) noexcept;
		friend bool operator!=(binary_operation_serdeser lhs, binary_operation_serdeser rhs) noexcept;
		friend binary_operation_serdeser& operator
			<<(binary_operation_serdeser& bosds, const binary_operation& bin_op);
		friend tostrm_t& operator<<(tostrm_t& ostr, const binary_operation_serdeser& other);		
		[[nodiscard]] bool has_value() const noexcept;
		[[nodiscard]] binary_operation value() const;
		[[nodiscard]] binary_operation value_or_default() const noexcept;
		const binary_operation& operator*() const noexcept;
		const binary_operation* operator->() const noexcept;
		operator bool() const noexcept;
		bool operator!() const noexcept;
		
		binary_operation_serdeser() noexcept = default;
		binary_operation_serdeser(const binary_operation_serdeser& other) noexcept = default;
		binary_operation_serdeser(binary_operation_serdeser&& other) noexcept = default;
		binary_operation_serdeser& operator=(const binary_operation_serdeser& other) noexcept = default;
		binary_operation_serdeser& operator=(binary_operation_serdeser&& other) noexcept = default;
		~binary_operation_serdeser() = default;
	private:
		const binary_operation* m_op_view;
	};
	
	
	struct cmd_args
	{
		friend bool operator==(const cmd_args& lhs, const cmd_args& rhs)
		{
			return lhs.m_num_ops == rhs.m_num_ops
				&& lhs.m_arr == rhs.m_arr;
		}

		friend bool operator!=(const cmd_args& lhs, const cmd_args& rhs) { return !(lhs == rhs); }
		[[nodiscard]] fsv_t first_file() const noexcept;
		[[nodiscard]] fsv_t second_file() const noexcept;
		[[nodiscard]] int op_count() const noexcept;
		[[nodiscard]] bool good() const noexcept;

		cmd_args(const fstr_arr_t& arr, int num_ops);
		~cmd_args() = default;
		cmd_args(const cmd_args& other) = default;
		cmd_args(cmd_args&& other) noexcept;
		cmd_args& operator=(const cmd_args& other) = default;
		cmd_args& operator=(cmd_args&& other) noexcept;
	private:
		cmd_args() noexcept : m_num_ops{}, m_arr{} {}
		int m_num_ops;
		fstr_arr_t m_arr;
	};

	class cjm_helper_rgen final
	{
	public:

		static std::unique_ptr<cjm_helper_rgen> make_rgen();

		binary_op random_binary_op();
		int128_t random_shift_arg();
		int128_t random_operand_arg();

		binary_operation random_operation(binary_op op);
		binary_operation random_operation();
		
				
		cjm_helper_rgen(const cjm_helper_rgen& other) = delete;
		cjm_helper_rgen(cjm_helper_rgen&& other) noexcept = delete;
		cjm_helper_rgen& operator=(const cjm_helper_rgen& other) = delete;
		cjm_helper_rgen& operator=(cjm_helper_rgen&& other) noexcept = delete;
		~cjm_helper_rgen() = default;
		
		
	private:
		cjm_helper_rgen();
		std::mt19937_64::result_type m_seed;
		std::random_device m_rnd;
		std::mt19937_64 m_twister;
		std::uniform_int_distribution<int> m_op_distrib;
		std::uniform_int_distribution<int> m_shift_distrib;
		std::uniform_int_distribution<std::int64_t> m_operand_distrib;
		
		
	};

	class bad_value_access final : public std::logic_error
	{
	public:	
		bad_value_access() : std::logic_error("The object accessed does not contain a valid value") {}
		~bad_value_access() override = default;
		bad_value_access(const bad_value_access& other) noexcept = default;
		bad_value_access(bad_value_access&& other) noexcept = delete;
		bad_value_access& operator=(const bad_value_access& other) noexcept = default;
		bad_value_access& operator=(bad_value_access&& other) noexcept = delete;
	};
	
	template<typename Char, typename CharTraits>
	std::vector<std::basic_string_view<Char, CharTraits>>
		split(std::basic_string_view<Char, CharTraits> split_me, Char split_on)
	{
		using char_t = Char;
		using traits_t = CharTraits;
		using sv_t = std::basic_string_view<char_t, traits_t>;
		using vec_t = std::vector<sv_t>;

		auto next_split = [](sv_t current, char_t c) -> std::pair<sv_t, sv_t>
		{
			auto ret = std::make_pair<sv_t, sv_t>(sv_t{}, sv_t{});
			if (!current.empty())
			{
				size_t idx = 0;
				bool done = false;
				while (!done)
				{
					if (current[idx++] == c)
					{
						sv_t split = current.substr(0, idx-1);
						sv_t remainder = idx < current.size() ? current.substr(idx) : sv_t{};
						ret.first = split;
						ret.second = remainder;
						done = true;
					}
					else
					{
						done = idx >= current.size();
					}					
				}
			}
			return ret;			
		};
		
		vec_t ret;
		sv_t split_next = split_me;
		while (!split_next.empty())
		{
			auto [split, remainder] = next_split(split_next, split_on);
			if (!split.empty())
			{
				ret.push_back(split);				
			}
			split_next = remainder;
		}
		return ret;
	}

	template <typename TSerDeser>
	tostrm_t& operator<<(tostrm_t& ost, const std::vector<binary_operation>& col)
	{
		static constexpr tsv_t item_delimiter = TSerDeser::item_delimiter;
		auto ser_deser = TSerDeser{};
		for (const binary_operation& op : col)
		{
			ser_deser << op;
			ost << ser_deser << item_delimiter;
		}
		return ost;
	}

	constexpr std::optional<tsv_t> text(binary_op op) noexcept
	{
		auto x = static_cast<unsigned int>(op);
		if (x < op_name_lookup.size())
		{
			return op_name_lookup[x];
		}
		return std::nullopt;
	}

	constexpr std::optional<binary_op> parse_op(tsv_t parse_me) noexcept
	{
		unsigned int idx = 0;
		for (const auto item : op_name_lookup)
		{
			if (parse_me == item)
			{
				return static_cast<binary_op>(idx);
			}
			++idx;
		}
		return std::nullopt;
	}

	
	static std::vector<binary_operation> init_edge_comparisons()
	{
		auto temp = std::array<int128_t, 11> 
			{	std::numeric_limits<int128_t>::max(),					std::numeric_limits<int128_t>::max() - 1,
				std::numeric_limits<int128_t>::min(),					std::numeric_limits<int128_t>::min() + 1,
				int128_t{std::numeric_limits<std::int64_t>::max()},	int128_t{std::numeric_limits<std::int64_t>::max() - 1},
				int128_t{std::numeric_limits<std::int64_t>::min()},	int128_t{std::numeric_limits<std::int64_t>::min() + 1},
				int128_t{0}, int128_t{1},
				int128_t{-1} };
		std::vector<binary_operation> store_permutations;
		store_permutations.reserve(11 * 11);
		for (size_t left_idx = 0; left_idx < temp.size(); ++left_idx)
		{
			for (size_t right_idx = 0; right_idx < temp.size(); ++right_idx)
			{
				auto op = binary_operation{ binary_op::compare, temp[left_idx], temp[right_idx], true };
				op.calculate_result();
				store_permutations.emplace_back(op);
			}
		}
		return store_permutations;
	}
}

namespace std
{
	template<>
	struct hash<cjm::binary_operation>
	{
		std::size_t operator()( const cjm::binary_operation& s) const noexcept
		{
			return hash_value(s);
		}
	};

	template<>
	struct hash<cjm::binary_operation_serdeser>
	{
		std::size_t operator()(cjm::binary_operation_serdeser s) const noexcept
		{
			return std::hash<const cjm::binary_operation*>{}(s.m_op_view);
		}
	};
}


#endif // CJM_HELPER_HPP_
