using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using static System.Console;
using System.IO;
using System.Text;


namespace othello
{
    using MoveMaker = Func<Board, char, char, (Board, bool)>;

    // Debugging class to generate visualizations to help fix some errors in the AI.
    static class DebugPrint {
        static StringBuilder str = new StringBuilder(10000);
        static int incrs = 0;

        public static void Increment() {
            incrs++;
        }
        public static void Decrement() {
            incrs--;
        }
        public static void Print(string s) {
            str.AppendFormat("{0} {1}\n", new string(' ', incrs * 4), s);
        }
        public static void Flush() {
            File.WriteAllText("debug.txt", str.ToString());
        }
    }
    static partial class Program
    {
        public const int MAXLEVEL = 4;

        // Read from STDIN and return the Position or the command that was passed in
        static (Position, string) GetInput() {
            var command = ReadLine().ToLower();

            if(command == "quit") {
                return (null, "quit");
            }


            var row = command[0] - 'a';
            var col = command[1] - 'a';

            if((uint) row >= Board.BOARD_X || (uint) col >= Board.BOARD_Y || command.Length != 2) {
                WriteLine($"Invalid input error: \"{command}\" ");
                return GetInput();
            }
            return (new Position(row, col), null);
        }

        // Map the white advantage to how favorable the position is for white
        static int white_compare(int x) {
            return x;
        }
        
        // Map the white advantage to how favorable the position is for black
        static int black_compare(int x) {
            return -x;
        }

        // Converts a string[,] to a char[,]
        static char[,] ToCharArr(string[,] arr) {
            int width = arr.GetLength(0);
            int height = arr.GetLength(1);
            char[,] ret = new char[width, height];

            for(int i = 0; i < width;i++) {
                for(int j=0;j<height;j++) {
                    Debug.Assert(arr[i,j].Length == 1);
                    ret[i,j]=arr[i,j][0];
                }
            }
            return ret;
        }
        public static string[,] FromCharArr(char[,] arr) {
            int width = arr.GetLength(0);
            int height = arr.GetLength(1);
            string[,] ret = new string[width, height];

            for(int i = 0; i < width;i++) {
                for(int j=0;j<height;j++) {
                    ret[i,j]= arr[i,j].ToString();
                }
            }
            return ret;
        }

        // Game is finished. Print score and end.
        static void Finish(Board board) {
            var white_adv = board.WhiteAdvantage();
            if (white_adv > 0) {
                WriteLine($"White wins by {white_adv}");
            } else if (white_adv < 0) {
                WriteLine($"Black wins by {-white_adv}");
            } else {
                WriteLine("Tie!");
            }
        }

        // Run the game, calling MoveMaker() methods for each turn.
        static void TwoPlayer(MoveMaker white_mover, MoveMaker black_mover)
        {
            var string_board = ToCharArr(Program.NewBoard(Board.BOARD_X, Board.BOARD_Y));

            var board = new Board(string_board);

            Console.WriteLine("White moves first! Type move to perform the move (if you are a human player)");
            board.Display();
            bool finished;
            while(true) {
                (board, finished) = white_mover(board, 'O', 'X');

                if(finished) {
                    Finish(board);
                    return;
                }
                board.Display();

                
                (board, finished) = black_mover(board, 'X', 'O');

                if(finished) {
                    Finish(board);
                    return;
                }
                board.Display();
            }
        }

        // MoveMaker method that calculates the best move and makes it, returning the board.
        static (Board, bool) MakeAiMove(Board board, char player, char opponent) {
            Func<int, int> mine, theirs;
            if (player == 'O') {
                mine = white_compare;
                theirs = black_compare;
            } else {
                mine = black_compare;
                theirs = white_compare;
            }
            var ai = new AIState(board, player, opponent, mine, theirs, -999999, 999999);
            var (move, _) = ai.Evaluate(7);

            if(move == null) {
                board.Display();
                WriteLine("\n\nAI found no more possible moves uwu. computer cwash...ending game :(.\n*:･ﾟ✧(ꈍᴗꈍ)✧･ﾟ:*\n");
                return (board, true);
            }
            var boardc = board.Clone();
            boardc.MakeMove(move, player, opponent);
            return (boardc, false);
        }

        // MoveMaker method that requests a move from the user, returning the new board
        static (Board, bool) MakePlayerMove(Board board, char player, char opponent) {
            WriteLine($"{player}'s move");
            var (pos, command) = GetInput();

            if(command != null) {
                return (null, true);
            }

            var boardc = board.Clone();
            if (!boardc.MakeMove(pos, player, opponent)) {
                WriteLine("Move error, make move again. Press 'quit' to quit and view score.");
                return MakePlayerMove(board, player, opponent);
            }

            boardc.Display();
            return (boardc, false);
        }

        // Prompt the user for what species they'd like to play as. 
        // They can play against another player, against the AI, or against an alien.
        // Returns an operator (Board -> Board) that returns the new board after making a
        static MoveMaker GetPlayerSpecies(string color) {
            Write($"Choose species for player {color} - homo sapien (h), Artificius Intelligus (a), or Alien (A)?\n(Type h/a/A or help): ");
            var species = ReadLine();

            if(species == "h") {
                return MakePlayerMove;
            } else if (species == "a") {
                return MakeAiMove;
            } else if (species == "A") {
                WriteLine(
                    "Your request is important, but all our available aliens are currently busy. " + 
                    "Please dial the intergalactic code - country code - area code " + 
                    "if you have a specific alien you'd like to play with. " + 
                    "Otherwise, please leave your message at the tone, and we will call you back.. Beeeeep.");
                Environment.Exit(0);
                return null;
            } else if (species == "help") {
                WriteLine("\n\nhomo sapien -- Play as a human. Make your own moves, your own choices, and deal with the consequences");
                WriteLine($"Artificius Intelligus -- Computer will play for {color}. It may make mistakes.");
                WriteLine($"Alien -- Alien will play for {color}");

                WriteLine("\nExamples: \n");
                WriteLine("If you (White) want to play against an AI (black), choose 'h' for white and 'a' for black\n");
                WriteLine("If you (White) want to play against a human friend (black), choose 'h' for white and 'h' for black\n");
                return GetPlayerSpecies(color);
            } else {
                throw new ArgumentException(
                    "You typed a species that I don't recognize."
                );
            }
        }

        // START HERE.
        // Submitted by Henry Nguyen for BME 121.
        // Extra features implemented: an AI.
        // AI notes: I coded a minimax, tree-search AI with culling. It tries to maximize material advantage (more of my tokens than the opponent's tokens)
        static void Main(string[] args)
        {
            // MoveMaker is a function type that returns a new board after making a valid move.
            MoveMaker white_move, black_move;
            white_move = GetPlayerSpecies("white");
            black_move = GetPlayerSpeciess("black");

            TwoPlayer(white_move, black_move);
        }
    }

    // Represents either a position vector or a direction vector.
    record Position(int row, int col);
    class Board {
        
        public const int BOARD_X = 8;
        public const int BOARD_Y = 8;
        const char WHITE='O';
        const char BLACK = 'X';
        public char[,] board;

        public int WhiteAdvantage() {
            var white_adv = 0;
            foreach(char s in board) {
                if (s == WHITE) white_adv ++;
                else if (s==BLACK) white_adv --;
            }
            return white_adv;
        }
        public Board (char[,] board)
        {
            this.board = board;
        }

        // Make a copy of the board to mutate
        public Board Clone() {
            var cloned = new Board((char[,]) this.board.Clone());
            return cloned;
        }

        // Mutate board and apply the move. Returns if that move was valid.
        public bool MakeMove(Position move, char player, char opponent) {
            // Check move
            Debug.Assert((uint) move.col < BOARD_Y && (uint) move.row < BOARD_X);
            if(board[move.row, move.col] != ' ') {
                WriteLine("Error board {0} {1}", move.row, move.col);
                WriteLine("Tried to make a move on an existing square");
                Display();
                return false;
            }

            board[move.row, move.col] = player;
            int flipped = 0;
            flipped += FlipOnDirection(new Position(-1, 0), move, opponent, player);

            flipped += FlipOnDirection(new Position(1, 0), move, opponent, player);

            flipped += FlipOnDirection(new Position(1, 1), move, opponent, player);

            flipped += FlipOnDirection(new Position(0, 1), move, opponent, player);

            flipped += FlipOnDirection(new Position(-1, 1), move, opponent, player);

            flipped += FlipOnDirection(new Position(-1, -1), move, opponent, player);

            flipped += FlipOnDirection(new Position(0, -1), move, opponent, player);

            flipped += FlipOnDirection(new Position(1, -1), move, opponent, player);

            if (flipped == 0) {
                board[move.row, move.col] = ' ';
                return false;
            }
            return true;
        }
        public bool ValidMove(Position move, char player, char opponent) {
            var board = Clone();
            return board.MakeMove(move, player, opponent);
        }

        public void Display()
        {
            WriteLine("\n");
            Program.DisplayBoard(Program.FromCharArr(board));
        }

        private int FlipOnDirection(Position vel, Position pos, char from, char to) {
            int flipped = 0;
            Debug.Assert(board[pos.row, pos.col] == to);

            var cur_row = pos.row;
            var cur_col = pos.col;
            while(
                // Position = position + velocity
                (uint) (cur_row += vel.row) < BOARD_X && (uint)(cur_col+=vel.col) < BOARD_Y && 
                // Check that the board is still 'O'
                board[cur_row,cur_col] == from) {
                flipped ++;
            }

            int flipped1 = flipped;
            // Going back
            if((uint) cur_row < BOARD_X && (uint) cur_col < BOARD_Y && board[cur_row,cur_col] == to) {

                while(flipped > 0) {
                    flipped -= 1;
                    board[cur_row -= vel.row, cur_col -= vel.col] = to;
                }
            } else {
                return 0;
            }
            return flipped1;
        }
    }


    // AIState encapsulates a board state, and calculates the tree of moves from that board state, and chooses the best possible move.
    class AIState {
        public Board board;
        public char player, opponent;
        public Func<int, int> my_comparator, enemy_comparator;

        public int depth_remaining;
        public static Dictionary<(char[,], char), Position> cache = new Dictionary<(char[,], char), Position>();

        public (Position, AIState) winning_state;
        public int chosen_score, alpha, beta;
        public int their_best;

        bool IsWhite() {
            return my_comparator(1000) == 1000;
        }
        public AIState(Board board, char player, char opponent, Func<int, int> my_comparator, Func<int, int> enemy_comparator,int alpha, int beta)
        {
            this.alpha = alpha;
            this.beta = beta;
            this.board = board;
            this.player = player;
            this.opponent = opponent;
            this.my_comparator = my_comparator;
            this.enemy_comparator    = enemy_comparator;
        }

        private List<Position> GenerateMoves() {
            bool check_surroundings(int i, int j, char opponent) {
                for(int x = -1; x <= 1; x++) {
                    for(int y = -1; y <= 1; y++) {
                        if((uint)(x+i) < Board.BOARD_X && (uint) (y+j) < Board.BOARD_Y) {
                            if(board.board[x + i, y + j] == opponent) {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }

            var res = new List<(int, Position)>();
            for(int i = 0; i < board.board.GetLength(0); i++) {
                for(int j = 0; j < board.board.GetLength(1); j++) {
                    if(board.board[i, j] == ' ') {
                        var OK = check_surroundings(i, j, opponent);
                        if (OK) {
                            var cloned = board.Clone();
                            if(cloned.MakeMove(new Position(i, j), player, opponent)) {
                                res.Add((my_comparator(cloned.WhiteAdvantage()), new Position(i, j)));
                            }
                        }
                    }
                }
            }

            res.Sort(delegate((int, Position) one, (int, Position) two) {
                return one.Item1.CompareTo(two.Item1);
            });
            res.Reverse();
            return res.Select(x => x.Item2).Take(8).ToList();
        }
        static Stack<(Position, int)> global_moves = new Stack<(Position, int)>();

        public void Options() {
            var possible_moves = GenerateMoves();
            foreach(var move in possible_moves) {
                var board_cloned = board.Clone();
                board_cloned.MakeMove(move, player, opponent);
                var new_ai = new AIState(board_cloned, opponent, player, enemy_comparator, my_comparator,  alpha, beta);
                global_moves.Push((move, 0));
                var (_, score) = new_ai.Evaluate(depth_remaining);
                global_moves.Pop();

                WriteLine("Option {0} Score {1} {2}", move, score, my_comparator(score));
            }
            WriteLine("Chosen move {0}", winning_state.Item1);

        }


        // Tree search and return the best move and the white advantage
        public (Position, int) Evaluate(int depth_remaining) {
            
            var possible_moves = GenerateMoves();


            if(possible_moves.Count == 0) {
                return (null, board.WhiteAdvantage());
            }
            if(depth_remaining <= 0) {
                // Return a random move and the board score at this state.
                var new_ai = new AIState(board, opponent, player, enemy_comparator, my_comparator, alpha, beta);
                winning_state = (possible_moves[0], new_ai);

                // foreach(var i in global_moves) {
                //     Write($"{i.Item1.row},{i.Item1.col} ");
                // }
                // WriteLine("Score {0}", board.WhiteAdvantage());
                return (possible_moves[0], board.WhiteAdvantage());
            }
            int max_score = -99999;

            if(!IsWhite()) {
                max_score *= -1;
            }

            Debug.Assert(my_comparator(max_score) == -99999);
            AIState best_ai = null;
            Position best_move = possible_moves[0];

            // Position cached_move;
            // if(cache.TryGetValue((board.board, player), out cached_move)) {
            //     WriteLine("Using cache");
            //     possible_moves = new List<Position> {cached_move};
            // }
            foreach(var move in possible_moves) {
                if(depth_remaining == Program.MAXLEVEL) {
                    // WriteLine("Considering move {0}", move);
                }
                var board_cloned = board.Clone();
                if(!board_cloned.MakeMove(move, player, opponent)) {
                    continue;
                }

                var new_ai = new AIState(board_cloned, opponent, player, enemy_comparator, my_comparator,  alpha, beta);

                DebugPrint.Print(String.Format("Option {0}", move));
                DebugPrint.Increment();
                var (_, score) = new_ai.Evaluate(depth_remaining - 1);
                DebugPrint.Decrement();

                
                if(IsWhite()) {
                    alpha = Math.Max(alpha,score);
                } else {
                    beta = Math.Min(beta,score);
                }

                if(alpha >= beta) {
                    break;
                }
                
                if(my_comparator(score) > my_comparator(max_score)) {
                    max_score = score;
                    best_ai = new_ai;
                    best_move = move;
                }
            }


            // if(!cache.ContainsKey((board.board, player))) {
            //     cache.Add((board.board, player), best_move);
            // }
            winning_state = (best_move, best_ai);
            chosen_score = max_score;

            return (best_move, max_score);
        }

        public void Playback() {
            if(winning_state.Item2 == null) {
            } else {
                WriteLine("Making move {0} {1} score {2}", Program.LetterAtIndex(winning_state.Item1.row), Program.LetterAtIndex(winning_state.Item1.col)
                , chosen_score);
                if(depth_remaining != Program.MAXLEVEL) Options();
                winning_state.Item2.board.Display();
                winning_state.Item2.Playback();
            }
        }
    }
}
