using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Linq; 

//  Enum để quản lý dữ liệu (Độ khó)
enum Difficulty { Easy = 1, Medium = 2, Hard = 3 }

// Struct để quản lý dữ liệu (Điểm số)
struct ScoreEntry
{
    public string Name;
    public Difficulty Difficulty;
    public int Moves;
    public double TimeSec;
}

class Program
{
    // Sử dụng const
    const string FILE_NAME = "scores.csv";

    //  Dùng Mảng 1 chiều + Biến đếm
    // Khởi tạo mảng ban đầu chứa được 10 phần tử (Capacity)
    static ScoreEntry[] Scores = new ScoreEntry[10];
    // Biến đếm số lượng phần tử thực tế đang có (Count)
    static int ScoreCount = 0;

    // Mảng 2 chiều (Ma trận đáp án) - Sẽ được sinh ngẫu nhiên
    static int[,] Solution = new int[9, 9];

    // Mảng 2 chiều (Ma trận đề bài - Có các ô trống số 0)
    static int[,] Puzzle = new int[9, 9];
    static Random rand = new Random();

    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.Unicode;

        //Tự động đọc dữ liệu khi mở game
        DocFileDiem();

        while (true)
        {

            // Menu console đầy đủ chức năng
            Console.Clear();
            Console.WriteLine("===== GAME SUDOKU =====");
            Console.WriteLine("1. Chơi game ");
            Console.WriteLine("2. Quản lý bảng điểm (Xem, Sửa, Xóa, Sắp xếp, Tìm kiếm)");
            Console.WriteLine("3. Lưu file dữ liệu");
            Console.WriteLine("4. Đọc lại file dữ liệu");
            Console.WriteLine("5. Thoát");
            Console.Write("Chọn chức năng: ");

            switch (Console.ReadLine())
            {
                case "1": ChoiGame(); break;
                case "2": QuanLyBangDiem(); break;
                case "3": LuuFileDiem(); TamDung(); break;
                case "4": DocFileDiem(); TamDung(); break;
                case "5": return; //Thoát an toàn
                default:
                    Console.WriteLine("Sai lựa chọn!");
                    TamDung();
                    break;
            }
        }
    }

    // =Tự quản lý cấp phát động (Thêm phần tử) 
    static void ThemDiem(ScoreEntry entry)
    {
        // Kiểm tra nếu mảng đầy (Count == Length)
        if (ScoreCount >= Scores.Length)
        {
            // Thuật toán cấp phát động: Tạo mảng mới gấp đôi kích thước cũ
            int newSize = Scores.Length == 0 ? 10 : Scores.Length * 2;
            ScoreEntry[] newArray = new ScoreEntry[newSize];

            // Copy dữ liệu từ mảng cũ sang mảng mới
            for (int i = 0; i < ScoreCount; i++)
            {
                newArray[i] = Scores[i];
            }

            // Trỏ mảng chính sang vùng nhớ mới
            Scores = newArray;
            // Console.WriteLine($"[System] Đã mở rộng bộ nhớ lên {newSize} phần tử.");
        }

        // Thêm phần tử vào vị trí cuối và tăng biến đếm
        Scores[ScoreCount] = entry;
        ScoreCount++;
    }

    // Tự quản lý xóa mảng (Dời phần tử) 
    static void XoaDiemTai(int index)
    {
        if (index < 0 || index >= ScoreCount) return;

        // Thuật toán xóa: Dời tất cả phần tử phía sau lên trước 1 bước
        for (int i = index; i < ScoreCount - 1; i++)
        {
            Scores[i] = Scores[i + 1];
        }

        // Giảm số lượng phần tử
        ScoreCount--;
        // Xóa dữ liệu rác ở vị trí cuối cùng (không bắt buộc nhưng nên làm)
        Scores[ScoreCount] = new ScoreEntry();
    }

    // ================= LOGIC của GAME =================
    static void ChoiGame()
    {
        Console.Clear();
        Console.Write("Nhập tên người chơi: ");
        string name = Console.ReadLine() ?? "NoName";

        Difficulty diff = ChonDoKho();
        TaoDuLieuGame(diff); // Sinh đề mới mỗi lần chơi

        // Copy mảng đề bài ra mảng chơi (tránh sửa trực tiếp đề gốc)
        int[,] board = (int[,])Puzzle.Clone();
        int moves = 0;
        int r = 0, c = 0; // Tọa độ con trỏ
        int soLanSai = 0;

        var sw = Stopwatch.StartNew(); // Bắt đầu tính giờ

        while (!KiemTraHoanThanh(board))
        {
            Console.Clear();
            HienHuongDan();
            Console.WriteLine($"Người chơi: {name} | Độ khó: {diff}");
            Console.WriteLine($"Số lần đoán sai: {soLanSai}/5"); // đoán sai tối đa 5 lần

            VeBanCo(board, r, c); // Vẽ bàn cờ

            Console.WriteLine($"Vị trí hiện tại: Hàng {r + 1}, Cột {c + 1}");
            Console.Write("Nhập số (1-9) hoặc di chuyển... ");

            var key = Console.ReadKey(true);

            // Xử lý phím điều khiển
            if (key.Key == ConsoleKey.Escape)
            {
                Console.WriteLine("\n Bạn đã thoát game — điểm KHÔNG được lưu.");
                TamDung();
                return;
            }
            if (key.Key == ConsoleKey.UpArrow && r > 0) r--;
            else if (key.Key == ConsoleKey.DownArrow && r < 8) r++;
            else if (key.Key == ConsoleKey.LeftArrow && c > 0) c--;
            else if (key.Key == ConsoleKey.RightArrow && c < 8) c++;
            else if (char.IsDigit(key.KeyChar))
            {
                int v = key.KeyChar - '0';

                // Không cho sửa ô đề bài
                if (Puzzle[r, c] != 0)
                {
                    BaoLoi("Ô đề bài — không thể sửa!");
                    continue;
                }

                // Xóa số (nhập 0)
                if (v == 0)
                {
                    board[r, c] = 0;
                    continue;
                }

                // Kiểm tra đáp án ngay lập tức
                if (Solution[r, c] == v)
                {
                    board[r, c] = v;
                    moves++;
                }
                else
                {
                    soLanSai++;
                    //  Gọi hàm Overloading nạp chồng
                    BaoLoi("Sai số — thử lại nhé!", ConsoleColor.Magenta);

                    if (soLanSai >= 5)
                    {
                        Console.Clear();
                        VeBanCo(board, -1, -1);
                        Console.WriteLine("\n GAME OVER! Bạn đã sai quá 5 lần.");
                        TamDung();
                        return;
                    }
                }
            }
        }

        sw.Stop();

        Console.Clear();
        VeBanCo(board, -1, -1);
        Console.WriteLine("CHÚC MỪNG — BẠN ĐÃ HOÀN THÀNH SUDOKU!");
        Console.WriteLine($"Thời gian: {sw.Elapsed.TotalSeconds:F1}s | Lượt đi: {moves}");

        // Thêm dữ liệu (Create) -> Dùng hàm tự viết
        ThemDiem(new ScoreEntry
        {
            Name = name,
            Difficulty = diff,
            Moves = moves,
            TimeSec = sw.Elapsed.TotalSeconds
        });
        Console.WriteLine("✔ Điểm của bạn đã được lưu vào danh sách tạm.");

        TamDung();
    }

    static Difficulty ChonDoKho()
    {
        Console.WriteLine("\nChọn độ khó:");
        Console.WriteLine("1. Easy (Dễ)");
        Console.WriteLine("2. Medium (Trung bình)");
        Console.WriteLine("3. Hard (Khó)");
        Console.Write("Chọn: ");

        string? input = Console.ReadLine();
        if (input == "2") return Difficulty.Medium;
        if (input == "3") return Difficulty.Hard;
        return Difficulty.Easy;
    }

    static void HienHuongDan()
    {
        Console.WriteLine("--- HƯỚNG DẪN ---");
        Console.WriteLine("↑ ↓ ← → : Di chuyển ô");
        Console.WriteLine("1–9     : Nhập số");
        Console.WriteLine("0       : Xoá số");
        Console.WriteLine("ESC     : Thoát game");
        Console.WriteLine("-----------------");
    }

    // ========= VẼ GIAO DIỆN =========
    static void VeBanCo(int[,] b, int cr, int cc)
    {
        Console.WriteLine("   ╔═══════════╦═══════════╦═══════════╗");
        for (int i = 0; i < 9; i++)
        {
            Console.Write("   ║");
            for (int j = 0; j < 9; j++)
            {
                bool isCursor = (i == cr && j == cc);
                bool isGiven = Puzzle[i, j] != 0;
                string v = b[i, j] == 0 ? " " : b[i, j].ToString();

                if (isCursor)
                {
                    Console.BackgroundColor = ConsoleColor.Yellow;
                    Console.ForegroundColor = ConsoleColor.Black;
                }
                else if (isGiven)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                }

                Console.Write($" {v} ");
                Console.ResetColor();
                Console.Write(j == 2 || j == 5 ? "║" : "│");
            }
            Console.WriteLine();
            if (i == 2 || i == 5)
                Console.WriteLine("   ╠═══════════╬═══════════╬═══════════╣");
            else if (i == 8)
                Console.WriteLine("   ╚═══════════╩═══════════╩═══════════╝");
            else
                Console.WriteLine("   ╟───────────┼───────────┼───────────╢");
        }
    }

    static bool KiemTraHoanThanh(int[,] b)
    {
        for (int i = 0; i < 9; i++)
            for (int j = 0; j < 9; j++)
                if (b[i, j] != Solution[i, j]) return false;
        return true;
    }

    // ================= QUẢN LÝ ĐIỂM SỐ =================
    static void QuanLyBangDiem()
    {
        while (true)
        {
            Console.Clear();
            HienBangDiem(); // Thống kê dữ liệu

            Console.WriteLine("1. Xóa người chơi");
            Console.WriteLine("2. Sửa tên người chơi ");
            Console.WriteLine("3. Sắp xếp theo thời gian ");
            Console.WriteLine("4. Tìm kiếm theo tên");
            Console.WriteLine("5. Quay lại");
            Console.Write("Chọn: ");

            switch (Console.ReadLine())
            {
                case "1": XoaNguoiChoi(); break;
                case "2": SuaTenNguoiChoi(); break;
                case "3": SapXepTheoThoiGian(); break;
                case "4": TimKiemTheoTen(); break;
                case "5": return;
            }
        }
    }

    static void HienBangDiem()
    {
        Console.WriteLine("===== BẢNG XẾP HẠNG =====");
        Console.WriteLine($"{"Tên",-15} | {"Độ khó",-10} | {"Lượt",-5} | {"Thời gian(s)",-10}");
        Console.WriteLine(new string('-', 50));
        // Duyệt mảng theo ScoreCount
        for (int i = 0; i < ScoreCount; i++)
        {
            var s = Scores[i];
            Console.WriteLine($"{s.Name,-15} | {s.Difficulty,-10} | {s.Moves,-5} | {s.TimeSec,-10:F1}");
        }
        Console.WriteLine();
    }

    //  Hàm dùng OUT param
    static bool TryGetScoreByName(string name, out int index)
    {
        // Duyệt mảng thủ công
        for (int i = 0; i < ScoreCount; i++)
        {
            if (Scores[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                return true;
            }
        }
        index = -1;
        return false;
    }

    // Thuật toán tìm kiếm tuyến tính (Linear Search)
    static void TimKiemTheoTen()
    {
        Console.Write("Nhập tên cần tìm: ");
        string key = Console.ReadLine() ?? "";

        if (TryGetScoreByName(key, out int i))
        {
            var s = Scores[i];
            Console.WriteLine($"✅ TÌM THẤY: {s.Name} | {s.Difficulty} | {s.TimeSec}s");
        }
        else
        {
            Console.WriteLine("❌ Không tìm thấy!");
        }
        TamDung();
    }

    // Chức năng XÓA dữ liệu
    static void XoaNguoiChoi()
    {
        Console.Write("Nhập tên cần xoá: ");
        string key = Console.ReadLine() ?? "";

        if (TryGetScoreByName(key, out int i))
        {
            // Gọi hàm xóa tự viết
            XoaDiemTai(i);
            Console.WriteLine("Đã xoá thành công.");
        }
        else
        {
            Console.WriteLine("Không tìm thấy tên này.");
        }
        TamDung();
    }

    //Chức năng SỬA dữ liệu 
    static void SuaTenNguoiChoi()
    {
        Console.Write("Nhập tên người chơi cần sửa: ");
        string oldName = Console.ReadLine() ?? "";

        // Tìm vị trí
        if (TryGetScoreByName(oldName, out int index))
        {
            Console.Write($"Đã tìm thấy '{Scores[index].Name}'. Nhập tên mới: ");
            string newName = Console.ReadLine()!;

            if (!string.IsNullOrWhiteSpace(newName))
            {
                // Vì ScoreEntry là Struct (Tham trị), phải lấy ra, sửa, rồi gán lại
                ScoreEntry temp = Scores[index];
                temp.Name = newName;
                Scores[index] = temp;
                Console.WriteLine("Đã cập nhật tên thành công!");
            }
            else
            {
                Console.WriteLine("Tên không được để trống.");
            }
        }
        else
        {
            Console.WriteLine("Không tìm thấy người chơi này.");
        }
        TamDung();
    }

    // Thuật toán sắp xếp nổi bọt (Bubble Sort)
    static void SapXepTheoThoiGian()
    {
        // Sắp xếp trên mảng Scores, giới hạn bởi ScoreCount
        for (int i = 0; i < ScoreCount - 1; i++)
        {
            bool swapped = false; // Cờ kiểm tra xem có hoán đổi nào xảy ra không
            for (int j = 0; j < ScoreCount - i - 1; j++)
            {
                if (Scores[j].TimeSec > Scores[j + 1].TimeSec)
                {
                    // Hoán đổi
                    var t = Scores[j];
                    Scores[j] = Scores[j + 1];
                    Scores[j + 1] = t;
                    swapped = true;
                }
            }
            // Nếu không có hoán đổi nào, mảng đã sắp xếp xong -> Dừng sớm
            if (!swapped) break;
        }
        Console.WriteLine("Đã sắp xếp theo thời gian (nhanh nhất -> chậm nhất).");
        TamDung();
    }

    // ================= FILE I/O =================

    // Hàm ghi file, tách I/O, tham số mặc định
    static void LuuFileDiem(string path = FILE_NAME)
    {
        try //Try-catch xử lý ngoại lệ
        {
            // Kiểm tra tồn tại và Tạo Backup 
            if (File.Exists(path))
                File.Copy(path, path + ".bak", true);

            using var w = new StreamWriter(path, false, Encoding.UTF8);
            // Duyệt mảng theo ScoreCount
            for (int i = 0; i < ScoreCount; i++)
            {
                var s = Scores[i];
                // Ghi dạng CSV: Ten,DoKho,Luot,ThoiGian
                w.WriteLine($"{s.Name},{s.Difficulty},{s.Moves},{s.TimeSec}");
            }
            Console.WriteLine("Đã lưu file (kèm backup).");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Lỗi lưu file: " + ex.Message);
        }
    }

    // Hàm đọc file
    static void DocFileDiem(string path = FILE_NAME)
    {
        try
        {
            if (!File.Exists(path)) return;

            // Reset lại biến đếm
            ScoreCount = 0;
            // Có thể reset lại mảng nếu muốn: Scores = new ScoreEntry[10];

            foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var p = line.Split(','); // tách chuỗi thành các phần tử cách nhau bởi dấu phẩy 

                // Parse dữ liệu từ file
                if (p.Length >= 4)
                {
                    // Dùng hàm ThemDiem để tự động resize nếu file quá lớn
                    ThemDiem(new ScoreEntry
                    {
                        Name = p[0],
                        Difficulty = Enum.Parse<Difficulty>(p[1]),
                        Moves = int.Parse(p[2]),
                        TimeSec = double.Parse(p[3])
                    });
                }
            }
            // Console.WriteLine("Đã tải dữ liệu.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Lỗi đọc file: " + ex.Message);
        }
    }

    // HÀM TỰ TẠO
    static void BaoLoi(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(msg);
        Console.ResetColor();
        Thread.Sleep(800);
    }

    //Hàm nạp chồng (Overloading)
    static void BaoLoi(string msg, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(msg);
        Console.ResetColor();
        Thread.Sleep(800);
    }

    static void TamDung()
    {
        Console.WriteLine("Nhấn Enter để tiếp tục...");
        Console.ReadLine();
    }

    // ================= THUẬT TOÁN SINH SUDOKU =================
    static void TaoDuLieuGame(Difficulty diff)
    {
        Solution = new int[9, 9];
        // 1. Điền số vào 3 khối chéo (độc lập nhau, không trùng hàng/cột)
        DienKhoiCheo();
        // 2. Dùng đệ quy quay lui (Backtracking) để điền nốt các ô còn lại
        GiaiSudoku(Solution);

        // 3. Sao chép sang Puzzle và đục lỗ (xóa số)
        Puzzle = (int[,])Solution.Clone();
        XoaSoTaoDe(diff);
    }

    static void DienKhoiCheo()
    {
        for (int i = 0; i < 9; i += 3)
        {
            DienMotKhoi3x3(i, i);
        }
    }

    static void DienMotKhoi3x3(int r, int c)
    {
        // Tạo danh sách 1-9 và xáo trộn ngẫu nhiên
        var nums = Enumerable.Range(1, 9).OrderBy(x => rand.Next()).ToArray();
        int idx = 0;
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                Solution[r + i, c + j] = nums[idx++];
    }

    static bool GiaiSudoku(int[,] board)
    {
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                if (board[r, c] == 0)
                {
                    for (int num = 1; num <= 9; num++)
                    {
                        if (KiemTraHopLe(board, r, c, num))
                        {
                            board[r, c] = num;
                            if (GiaiSudoku(board)) return true;
                            board[r, c] = 0; // Quay lui
                        }
                    }
                    return false;
                }
            }
        }
        return true;
    }

    static bool KiemTraHopLe(int[,] board, int r, int c, int num)
    {
        // Kiểm tra hàng và cột
        for (int i = 0; i < 9; i++)
            if (board[r, i] == num || board[i, c] == num) return false;

        // Kiểm tra khối 3x3
        int startR = r - r % 3;
        int startC = c - c % 3;
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                if (board[startR + i, startC + j] == num) return false;

        return true;
    }

    static void XoaSoTaoDe(Difficulty diff)
    {
        int soOcanXoa = diff switch
        {
            Difficulty.Easy => 30,    // Xóa 30 ô -> còn 51 ô
            Difficulty.Medium => 45,  // Xóa 45 ô -> còn 36 ô
            Difficulty.Hard => 55,    // Xóa 55 ô -> còn 26 ô
            _ => 30
        };

        while (soOcanXoa > 0)
        {
            int r = rand.Next(9);
            int c = rand.Next(9);
            if (Puzzle[r, c] != 0)
            {
                Puzzle[r, c] = 0;
                soOcanXoa--;
            }
        }
    }
}