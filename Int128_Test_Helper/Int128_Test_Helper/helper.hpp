#ifndef CJM_HELPER_HPP_
#define CJM_HELPER_HPP_
#include <string_view>
#include <string>
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
namespace cjm
{
	using namespace std::string_view_literals;
	constexpr auto newl = '\n';
	constexpr auto w_newl = u'\n';

	using uint128_t = absl::uint128;
	using int128_t = absl::int128;
	using fstr_t = std::string;
	using fstr_arr_t = std::array<fstr_t, 2>;
	using fsv_t = std::string_view;
	using tsv_t = std::u16string_view;

	
	
	enum binary_op : int
	{
		LeftShift = 0,
		RightShift,
		And,
		Or,
		Xor,
		
		Divide,
		Modulus,
		Add,
		Subtract,
		Multiply
	};
	struct binary_operation;
	class cjm_helper_rgen;
	struct cmd_args;
	
	std::vector<binary_operation> create_random_ops(size_t count);
	std::vector<binary_operation> create_random_ops(size_t count, binary_op op_code);
	int execute(int argc, char* argv[]);
	cmd_args extract_arr(int argc, char* argv[]);
	constexpr std::optional<tsv_t> text(binary_op op) noexcept;
		
	constexpr std::array<tsv_t, 10> op_name_lookup =
		std::array<tsv_t, 10>{
		u"LeftShift"sv, u"RightShift"sv,
			u"And"sv, u"Or"sv,
			u"Xor"sv, u"Divide"sv,
			u"Modulus"sv, u"Add"sv,
			u"Subtract"sv, u"Multiply"sv, };
	

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
		int128_t m_rhs;
		int128_t m_lhs;
		std::optional<int128_t> m_result;
		
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
	
	constexpr std::optional<tsv_t> text(binary_op op) noexcept
	{
		size_t x = static_cast<size_t>(op);
		if (x < op_name_lookup.size())
		{
			return op_name_lookup[x];
		}
		return std::nullopt;
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
}


#endif // CJM_HELPER_HPP_
