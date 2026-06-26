using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
using System.IO;
using System.Collections.Generic;
using SortGems.Core;
using SortGems.UI;
using SortGems.Ads;

/// <summary>
/// モバイル縦画面（1080×1920）レイアウトでゲームシーンを自動生成する。
///
/// レイアウト構成:
///   [UIToolkit]       — UIDocument + ScreenManager（Title/StageSelect/GamePlayHUD画面遷移）
///   [Canvas]          — uGUI Canvas（パズルグリッド描画専用）
///     GamePlayPanel   — GridContainer + PaletteContainer（uGUIタッチ操作用）
/// </summary>
public class CreateGameScene : EditorWindow
{
    // ---- レイアウト定数 ----
    const int REF_W         = 1080;
    const int REF_H         = 1920;
    const int HEADER_H      = 120;
    const float CELL_SIZE   = 41.5f;
    const float CELL_SPACING = 0f;
    const float PALETTE_GAP  = 24f;   // グリッドとパレットの隙間
    const int BUTTON_H      = 80;

    // ---- ステージ定数 ----
    const int MAIN_ROWS    = 16;
    const int MAIN_COLS    = 16;
    const int PAL_ROWS     = 2;
    const int PAL_COLS     = 14;

    // ---- 16x16 ピクセルアート定義 ----
    // R=Red B=Blue G=Green Y=Yellow P=Purple O=Orange C=Cyan K=Pink
    // W=White D=Black N=Brown L=LightBlue .=空

    // Stage 1: スター（五芒星）
    private static readonly string[] StarArt = new string[]
    {
        ".......DD.......",
        "......DYYD......",
        ".....DYYYYD.....",
        ".....DYYYYD.....",
        "DDDDDYYYYYYDDDD.",
        ".DYOOYYYYYOOYD..",
        "..DYOOYYYOOY....",
        "...DYYYYYYY.....",
        "..DYOOYYYOOY....",
        "..DYYD...DYYD...",
        ".DYD.......DYD..",
        ".DD.........DD..",
        "................",
        "................",
        "................",
        "................"
    };

    // Stage 2: ハート
    private static readonly string[] HeartArt = new string[]
    {
        "................",
        "..DKK.....KKD...",
        ".DKWKK...KKWKD..",
        "DKRRRRR.RRRRRKKD",
        "DRRRRRRRRRRRRRRD",
        "DRRRRRRRRRRRRRRD",
        "DRRRRRRRRRRRRRRD",
        ".DRRRRRRRRRRRRD.",
        "..DRRRRRRRRRRD..",
        "...DRRRRRRRRD...",
        "....DRRRRRRD....",
        ".....DRRRRD.....",
        "......DRRD......",
        ".......DD.......",
        "................",
        "................"
    };

    // Stage 3: 木（リンゴ付き）
    private static readonly string[] TreeArt = new string[]
    {
        ".......GG.......",
        "......DGGGD.....",
        ".....DGGGGGGD...",
        "....DGGRGGGGGGD.",
        "...DGGGGGGGGGGD.",
        "...DGGGGGGRGGD..",
        "..DGGGGGGGGGGD..",
        "...DGGRGGGGGGD..",
        "....DGGGGGGD....",
        ".....DGGGGD.....",
        ".......NN.......",
        ".......NN.......",
        "......NNNN......",
        ".....NNNNNN.....",
        "....NNNNNNNN....",
        "................"
    };

    // Stage 4: ひまわり
    private static readonly string[] FlowerArt = new string[]
    {
        ".....YYYYY......",
        "...YY.YYYYY.YY..",
        "..YYYYYYYYYYYYY.",
        "..YYYYDNNNDYYYY.",
        ".YYYYYNNNNNYYYYY",
        ".YYYYYNNNNNYYYYY",
        "..YYYYDNNNDYYYY.",
        "..YYYYYYYYYYYYY.",
        "...YY.YYYYY.YY..",
        ".......GGG......",
        "......GGGG......",
        ".....GG.GGG.....",
        "....GG...GG.....",
        "....G.....G.....",
        "................",
        "................"
    };

    // Stage 5: ネコ
    private static readonly string[] CatArt = new string[]
    {
        "..OD.........DO.",
        ".OOKD.......DKOO",
        ".OOOOO.....OOOOO",
        ".OOOOOOOOOOOOOOO",
        ".OOOOOOOOOOOOOOO",
        ".ODWOOOOOOOODWOO",
        ".OOOOOOOOOOOOOOO",
        ".OOOOODNNDOOOOO.",
        "..OOOOOOOOOOOOO.",
        "...OOOOOOOOOOO..",
        "....OOOOOOOOO...",
        "................",
        ".....OOOOOOO....",
        "....OOOOOOOOO...",
        "....OOOOOOOOO...",
        ".....OOOOOOO...."
    };

    // Stage 6: イヌ
    private static readonly string[] DogArt = new string[]
    {
        "..NNNN....NNNN..",
        ".NNNNNN..NNNNNN.",
        ".NNNNNN..NNNNNN.",
        "..NNNNNNNNNNNN..",
        "..NNNNNNNNNNNN..",
        "..NDWNNNNNNDWN..",
        "..NNNNNNNNNNNN..",
        "..NNNNDDNNNNNN..",
        "..NNNNNNNNNNNN..",
        "...NNNNNNNNNN...",
        "....NNNNNNNN....",
        "....NN....NN....",
        "....NN....NN....",
        "....NN....NN....",
        "...NNN....NNN...",
        "................"
    };

    // Stage 7: リンゴ
    private static readonly string[] AppleArt = new string[]
    {
        ".......NGG......",
        "......GGG.......",
        ".....DRRRRRD....",
        "...DRRWRRRRRRD..",
        "..DRRWRRRRRRRRD.",
        ".DRRRRRRRRRRRRRD",
        ".DRRRRRRRRRRRRRD",
        ".DRRRRRRRRRRRRRD",
        ".DRRRRRRRRRRRRRD",
        ".DRRRRRRRRRRRRRD",
        "..DRRRRRRRRRRRD.",
        "..DRRRRRRRRRRRD.",
        "...DRRRRRRRRD...",
        "....DRRRRRD.....",
        "................",
        "................"
    };

    // Stage 8: サカナ
    private static readonly string[] FishArt = new string[]
    {
        "................",
        "......LLBB......",
        "....LLBBBBBB....",
        "...LBBBBBBBBBD..",
        "..LBBBBBBBBBBBD.",
        ".LBWBBBBBBBBBBD.",
        "LBBBBYBBBBBBBLBD",
        "LBBBBBBBBBBBLBD.",
        "LBBBBBBBBBBLBD..",
        ".LBBBBBBBBBLBD..",
        "..LBBBBBBBBD....",
        "...LBBBBBBBD....",
        "....LLBBBBD.....",
        "......LLBD......",
        "................",
        "................"
    };

    // Stage 9: チョウチョ
    private static readonly string[] ButterflyArt = new string[]
    {
        "................",
        ".PPP.........PPP",
        "PPWPPP.....PPPWP",
        "PPKPPPP...PPPPKP",
        "PPWPPPPP.PPPPPWP",
        "PPKPPPPP.PPPPPKP",
        "PPPPPPPPDPPPPPP.",
        ".PPPPPPPDDPPPPP.",
        "..PPPPPPDDPPPP..",
        "...PPPPPDDPPP...",
        "....PPPPDDPP....",
        ".....PPPDDP.....",
        "......DDDD......",
        ".......DD.......",
        "................",
        "................"
    };

    // Stage 10: ロケット
    private static readonly string[] RocketArt = new string[]
    {
        ".......WW.......",
        "......WCWW......",
        ".....WCCCCW.....",
        ".....CCCCCC.....",
        "....DCCCCCCDD...",
        "....DCCLCCCD....",
        "....DCCLCCCD....",
        "....DCCCCCCDD...",
        "....DCCCCCCDD...",
        "...RDCCCCCCDRD..",
        "..RRDCCCCCCDRRR.",
        "..RRDCCCCCCDRRR.",
        ".RRR.CCCCCC.RRR.",
        ".RR..OOOOOO..RR.",
        ".R...OYOOYO...R.",
        "......YOOY......"
    };

    // Stage 11: おうち
    private static readonly string[] HouseArt = new string[]
    {
        "........R.......",
        ".......RRR......",
        "......RRRRR.....",
        ".....RRRRRRR....",
        "....RRRRRRRRR...",
        "...RRRRRRRRRRR..",
        "..DNNNNNNNNNNND.",
        "..NNNNNNNNNNNNN.",
        "..NNLLNNNLLNNN..",
        "..NNLLNNNLLNNN..",
        "..NNNNNNNLLNNN..",
        "..NNNNNNNNNNNNN.",
        "..NNNNNDNNNNNNN.",
        "..NNNNNDDNNNNN..",
        "..NNNNNDDNNNNN..",
        "..NNNNNNNNNNNNN."
    };

    // Stage 12: ニコちゃん（スマイリー）
    private static readonly string[] SmileyArt = new string[]
    {
        "....YYYYYYYY....",
        "..YYYYYYYYYYYY..",
        ".YYYYYYYYYYYYYY.",
        ".YYYYYYYYYYYYYY.",
        "YYYYDYYYYYYYYDYY",
        "YYYYDYYYYYYYYDYY",
        ".YYYYYYYYYYYYYY.",
        ".YYYYYYYYYYYYYY.",
        ".YYYYYYYYYYYYYY.",
        ".YYYDYYYYYYYDY..",
        "..YYYYDYYYYDYYY.",
        "..YYYYYDDDDDYY..",
        "...YYYYYYYYYY...",
        "....YYYYYYYY....",
        "................",
        "................"
    };

    // Stage 13: きのこ
    private static readonly string[] MushroomArt = new string[]
    {
        "......RRRR......",
        "....DRRRRRRRD...",
        "...DRRWRRRRWRD..",
        "..DRRWRRRRRWRRD.",
        ".DRRRRRRRRRRRRD.",
        ".DRRRRRRRRRRRRD.",
        "DRRRRRRRRRRRRRD.",
        "DDDDDDDDDDDDDDDD",
        "......NNNN......",
        ".....WNNNWW.....",
        ".....NNNNNN.....",
        ".....NNNNNN.....",
        ".....NNNNNN.....",
        "....NNNNNNNN....",
        "...NNNNNNNNNN...",
        "................"
    };

    // Stage 14: くるま
    private static readonly string[] CarArt = new string[]
    {
        "................",
        "................",
        "................",
        ".....DRRRRRD....",
        "....DRRRRRRRD...",
        "...DRRLRRRLRD...",
        "..DRRRRRRRRRRRD.",
        ".DRRRRRRRRRRRRD.",
        ".DDDDDDDDDDDDDDD",
        ".DRRRRRRRRRRRRD.",
        "..DDKDRRDDDKDD..",
        ".DDDDD..DDDDDD..",
        ".DDDDD..DDDDDD..",
        "..DDD....DDD....",
        "................",
        "................"
    };

    // Stage 15: かさ
    private static readonly string[] UmbrellaArt = new string[]
    {
        ".......NN.......",
        "....PPPPPPPP....",
        "..PPPPPPPPPPPP..",
        ".PPPPPPPPPPPPPP.",
        "DPPDPPDPPDPPDPPD",
        "PPPPPPPPPPPPPPPP",
        "PP.PPP.PPP.PPP.P",
        ".......NN.......",
        ".......NN.......",
        ".......NN.......",
        ".......NN.......",
        ".......NN.......",
        ".......NN.......",
        ".......NNN......",
        "........NN......",
        "................"
    };

    // Stage 16: ケーキ
    private static readonly string[] CakeArt = new string[]
    {
        ".......YY.......",
        ".......OO.......",
        "......KKKK......",
        "....KWKWKWKK....",
        "...KKKKKKKKKK...",
        "..RRRRRRRRRRRR..",
        "..KKKKKKKKKKKK..",
        "..KWKWKWKWKWKK..",
        "..RRRRRRRRRRRR..",
        "..NNNNNNNNNNNN..",
        "..NWNWNWNWNWNN..",
        "..NNNNNNNNNNNN..",
        "..RRRRRRRRRRRR..",
        "..NNNNNNNNNNNN..",
        "..NNNNNNNNNNNN..",
        "..DDDDDDDDDDDD.."
    };

    // Stage 17: おばけ
    private static readonly string[] GhostArt = new string[]
    {
        ".....WWWWWW.....",
        "...WWWWWWWWWW...",
        "..WWWWWWWWWWWW..",
        ".WWWWWWWWWWWWWW.",
        ".WWWDWWWWWWDWWW.",
        ".WWWDWWWWWWDWWW.",
        ".WWWWWWWWWWWWWW.",
        ".WWWWWWDWWWWWWW.",
        ".WWWWWWWWWWWWWW.",
        ".WWWWWWWWWWWWWW.",
        ".WWWWWWWWWWWWWW.",
        ".WWWWWWWWWWWWWW.",
        ".WWW.WWWWWW.WWW.",
        ".WW...WWWW...WW.",
        "................",
        "................"
    };

    // Stage 18: かえる
    private static readonly string[] FrogArt = new string[]
    {
        "..GGG......GGG..",
        ".GGGGG....GGGGG.",
        ".GDGGG....GGGDG.",
        ".GGGGG....GGGGG.",
        "..GGGGGGGGGGGG..",
        "..GGGGGGGGGGGG..",
        "..GGGGRRRGGG....",
        "..GGGGGGGGGGGG..",
        "..GGGGGGGGGGGG..",
        "...GGGGGGGGGG...",
        "....GGGGGGGG....",
        "................",
        ".GG..........GG.",
        ".GGGG......GGGG.",
        "..GGGG....GGGG..",
        "...GG......GG..."
    };

    // Stage 19: ペンギン
    private static readonly string[] PenguinArt = new string[]
    {
        ".....DDDDDD.....",
        "....DDDDDDDD....",
        "...DDDDDDDDDD...",
        "...DDDWWWWDDD...",
        "..DDDWWWWWWDDD..",
        "..DDWWDWWDWWDD..",
        "..DDDWWWWWWDDD..",
        ".DDDDWWOOWWDDDD.",
        ".DDDDWWWWWWDDDD.",
        ".DDDDDWWWWDDDDD.",
        "..DDDDDDDDDDDD..",
        "...DDDDDDDDDD...",
        "....DDDDDDDD....",
        "....OOO..OOO....",
        "................",
        "................"
    };

    // Stage 20: たいよう
    private static readonly string[] SunArt = new string[]
    {
        ".......YY.......",
        "..Y....YY....Y..",
        "..YY..YYYY..YY..",
        "...YYYYYYYYYY...",
        "...YYYOOOYYY....",
        "..YYYYOOOOYYYY..",
        "YYYYYOOWOOOYYYYY",
        "YYYYYOOWOOOYYYYY",
        "YYYYYOOOOOOYYYY.",
        "..YYYYOOOOYYYY..",
        "...YYYOOOYYY....",
        "...YYYYYYYYYY...",
        "..YY..YYYY..YY..",
        "..Y....YY....Y..",
        ".......YY.......",
        "................"
    };

    // Stage 21: かたつむり
    private static readonly string[] SnailArt = new string[]
    {
        "................",
        "....DD..........",
        "...DWWD.........",
        "....DD..........",
        "....OOOOOO......",
        "...OOOOOOOOO....",
        "..OOONNNNOOOOO..",
        ".OOOONWWNOOOOOO.",
        ".OOOONNNNOOOOOO.",
        ".OOOONNNNOOOOOO.",
        ".OOOONNNNOOOOO..",
        "..OOOOOOOOOOO...",
        "...OOOOOOOOO....",
        "GGGGGGGGGGGGGGGG",
        "NNNNNNNNNNNNNNNN",
        "................"
    };

    // Stage 22: にじ
    private static readonly string[] RainbowArt = new string[]
    {
        "................",
        "....RRRRRRRR....",
        "..RRRRRRRRRRRR..",
        ".RROOOOOOOOORR..",
        ".ROOYYYYYYYOOR..",
        "ROOYGGGGGGYYOOR.",
        "ROYGBBBBBBGYOOR.",
        "ROYGBPPPPPBGYOR.",
        "ROYGBP....PBGYOR",
        "ROYG........GYOR",
        "ROY..........YOR",
        "RO............OR",
        "R..............R",
        "................",
        "..WW......WW....",
        ".WWWW....WWWW..."
    };

    // Stage 23: ダイヤモンド
    private static readonly string[] DiamondArt = new string[]
    {
        "................",
        "......LLLL......",
        ".....LCLLCL.....",
        "....LCCCCCCCL...",
        "...LCCWCCWCCL...",
        "..LCCCWCCCCWCL..",
        ".LCCCCCCCCCCCCL.",
        "LCCCCCCCCCCCCCCD",
        ".LCCCCCCCCCCCCL.",
        "..LCCCCCCCCCCL..",
        "...LCCCCCCCCL...",
        "....LCCCCCLL....",
        ".....LCCCCL.....",
        "......LCCL......",
        ".......LL.......",
        "................"
    };

    // Stage 24: おんぷ
    private static readonly string[] MusicNoteArt = new string[]
    {
        "................",
        "....DDDDDDDDDDD.",
        "....DDWWWWWWWDD.",
        ".....DD......DD.",
        ".....DD.......DD",
        ".....DD.......DD",
        ".....DD.......DD",
        ".....DD.......DD",
        ".....DD.......DD",
        "...DDDDD...DDDDD",
        "..DDDDDDD.DDDDDD",
        ".DDDDDDDDDDDDDDD",
        ".DDDDDDD.DDDDDDD",
        "..DDDDD...DDDDD.",
        "................",
        "................"
    };

    // Stage 25: ヨット
    private static readonly string[] SailboatArt = new string[]
    {
        "........W.......",
        "........WW......",
        "........WWW.....",
        "........WWWW....",
        "........WWWWW...",
        "........WWWWWW..",
        "...R....WWWWWWW.",
        "...RR...WWWWWW..",
        "...RRR..WWWWW...",
        "...RRRR.WWWW....",
        "...RRRRRWWWW....",
        "..NNNNNNNNNNN...",
        ".NNNNNNNNNNNNN..",
        "BBBBBBBBBBBBBBB.",
        ".BLLLLLLLLLLLL..",
        "..BBBBBBBBBBB..."
    };

    // Stage 26: クラウン（王冠）
    private static readonly string[] CrownArt = new string[]
    {
        "................",
        "..Y...YY.Y...Y..",
        "..YY.YYYY.YY.Y..",
        "..YYYYYYYYYYYY..",
        "..YYYYYYYYYYYY..",
        "..YYYYYYYYYYYY..",
        "..YRYYYYYYYYRY..",
        "..YYYYYYYYYYYY..",
        "..YYYYYYYYYYYY..",
        "..YYYYYYYYYYYY..",
        "..YYYYYYYYYYYY..",
        "..DRRRRRRRRRYD..",
        "..YYYYYYYYYYYY..",
        "..DDDDDDDDDDDD..",
        "................",
        "................"
    };

    // Stage 27: カップケーキ
    private static readonly string[] CupcakeArt = new string[]
    {
        ".......RR.......",
        "......KKKK......",
        ".....KWKWKK.....",
        "....KKKKKKKK....",
        "...KKYKKYKKK....",
        "...KKKKKKKKKK...",
        "..DKKKKKKKKKKD..",
        "...NNNNNNNNNN...",
        "...NWNWNWNWNN...",
        "....NNNNNNNN....",
        "....NNNNNNNN....",
        "....NNNNNNNN....",
        "....NNNNNNNN....",
        ".....NNNNNN.....",
        "......DDDD......",
        "................"
    };

    // Stage 28: スイカ
    private static readonly string[] WatermelonArt = new string[]
    {
        "......GGGG......",
        "....GGGGGGGGG...",
        "...GGGGGGGGGGG..",
        "..GGDRRRRRRRDG..",
        ".GGDRRRRRRRRRDG.",
        ".GDRRDRRRDRRRDG.",
        ".GDRRRRRRRRRRRDG",
        "GDRRRRDRRRDRRRDG",
        "GDRRRRRRRRRRRRRD",
        "GDRRRRDRRRRRDRRG",
        ".GDRRRRRRRRRRRDG",
        ".GGDRRRRRRRRRDG.",
        "..GGDRRRRRRRDG..",
        "...GGGGGGGGGGG..",
        "....GGGGGGGGG...",
        "................"
    };

    // Stage 29: カメ
    private static readonly string[] TurtleArt = new string[]
    {
        "................",
        "................",
        ".....DGGGGGGD...",
        "...DGGGGGGGGGD..",
        "..DGYGGYGGYGGD..",
        ".DGGGGGGGGGGGDG.",
        ".DGYGGYGGYGGD...",
        "GGGDGGGGGGGDGGGG",
        "GGGGGDDDDDGGGGGG",
        ".GG.GGGGGGGGG.GG",
        "....GGGGGGGGG...",
        "..GG.GGGGGGG.GG.",
        "..GG..GGGGG..GG.",
        "......GGGGG.....",
        "................",
        "................"
    };

    // Stage 30: ほし月
    private static readonly string[] MoonStarArt = new string[]
    {
        "..........YY....",
        "..PPPPPP.YOOY...",
        ".PPPPPPPP.YY....",
        "PPWPPPPP........",
        "PPPPPPP.........",
        "PPPPPPP.........",
        "PPPPPPP.........",
        "PPPPPPPP........",
        ".PPWPPPPP.......",
        ".PPPPPPPPP......",
        "..PPPPPPPPP.....",
        "...PPPPPPPP.....",
        "....PPPPPP......",
        ".....PPPP.......",
        "................",
        "................"
    };

    // ---- 18x18 ピクセルアート ----

    // Stage 31: パンダ (18x18)
    private static readonly string[] PandaArt18 = new string[]
    {
        "..................",
        "....DDDDDDDDD.....",
        "..DDDWWWWWWWDDD...",
        ".DDDWWWWWWWWWDDD..",
        ".DDWWWWWWWWWWWDD..",
        "DDDWWWWWWWWWWWDDD.",
        "DDWWWDWWWWWDWWWDD.",
        "DDWWWDWWWWWDWWWDD.",
        "DDWWWWWWWWWWWWWDD.",
        "DDWWWWWWDWWWWWWDD.",
        ".DDWWWWWWWWWWWDD..",
        ".DDDWWDDDDDWDDD...",
        "..DDDWWWWWWWDDD...",
        "....DDDDDDDDD.....",
        "...DD.......DD....",
        "..DDD.......DDD...",
        "..DD.........DD...",
        ".................."
    };

    // Stage 32: クジラ (18x18)
    private static readonly string[] WhaleArt18 = new string[]
    {
        "..................",
        "........LL........",
        "......LLLLLL......",
        ".....LBBBBBBBL....",
        "...LBBBBBBBBBBL...",
        "..LBBBBBBBBBBBBBL.",
        ".LBBWBBBBBBBBBBBL.",
        "LBBBBBBBBBBBBBBBL.",
        "LBBBBBBBBBBBBBBBL.",
        "LBBBBBBBBBBBBBBL..",
        ".LBBBBBBBBBBBBBL..",
        "..LLBBBBBBBBLL....",
        "....LLBBBBLL......",
        "......LLLL........",
        ".........LL.......",
        "..........LL......",
        "..................",
        ".................."
    };

    // Stage 33: ドーナツ (18x18)
    private static readonly string[] DonutArt18 = new string[]
    {
        "..................",
        "......KKKKKK......",
        "....KKKKKKKKKKK...",
        "...KKKKKKKKKKKKK..",
        "..KKKKKKKKKKKKKK..",
        ".KKKKKKKKKKKKKKKK.",
        ".KKKKK......KKKKK.",
        ".KKKKK......KKKKK.",
        ".NNNN........NNNN.",
        ".NNNN........NNNN.",
        ".NNNNN......NNNNN.",
        ".NNNNN......NNNNN.",
        "..NNNNNNNNNNNNNN..",
        "..NNNNNNNNNNNNNNN.",
        "...NNNNNNNNNNNNN..",
        "....NNNNNNNNNNN...",
        "......NNNNNN......",
        ".................."
    };

    // ---- 24x24 ピクセルアート ----

    // Stage 34: フクロウ (24x24)
    private static readonly string[] OwlArt24 = new string[]
    {
        "........................",
        "........................",
        "........NNNNNNNN........",
        "......NNNNNNNNNNNN......",
        ".....NNNNNNNNNNNNNN.....",
        "....NNNNNNNNNNNNNNNN....",
        "...NNNNNNNNNNNNNNNNNN...",
        "..NNNNDWWNNNNNDWWNNN....",
        "..NNNDWWWNNNNDWWWNNN....",
        "..NNNWWDWNNNNWWDWNNN....",
        "..NNNDWWWNNNNDWWWNNN....",
        "..NNNNDWWNNNNNDWWNNN....",
        "...NNNNNNNNNNNNNNNN.....",
        "...NNNNNNNOONNNNNNN.....",
        "....NNNNNNNNNNNNNN......",
        "....NNNNDDDDDNNNNN......",
        ".....NNNNNNNNNNNN.......",
        "......NNNNNNNNNN........",
        ".......NNNNNNNN.........",
        ".......NN....NN.........",
        "......NNN....NNN........",
        "......NNN....NNN........",
        "........................",
        "........................"
    };

    // Stage 35: ヨットと海 (24x24)
    private static readonly string[] SailboatArt24 = new string[]
    {
        "........................",
        "............W...........",
        "............WW..........",
        "............WWW.........",
        "............WWWW........",
        "............WWWWW.......",
        "............WWWWWW......",
        ".....R......WWWWWWW.....",
        ".....RR.....WWWWWW......",
        ".....RRR....WWWWW.......",
        ".....RRRR...WWWW........",
        ".....RRRRRRRWWWW........",
        "....NNNNNNNNNNNNN.......",
        "...NNNNNNNNNNNNNNN......",
        "..BBBBBBBBBBBBBBBBB.....",
        "..BLLLLLLLLLLLLLLBB.....",
        "...BBBBBBBBBBBBBBB......",
        "LLLLLLLLLLLLLLLLLLLLLLLL",
        "BBBBBBBBBBBBBBBBBBBBBBBB",
        "LLLLLLLLLLLLLLLLLLLLLLLL",
        "BBBBBBBBBBBBBBBBBBBBBBBB",
        "........................",
        "........................",
        "........................"
    };

    // Stage 36: ロボット (24x24)
    private static readonly string[] RobotArt24 = new string[]
    {
        "........................",
        "..........DD............",
        ".........DDDD...........",
        ".......DDDDDDDD.........",
        "......DDDDDDDDDD........",
        ".....DDDDDDDDDDDDD......",
        ".....DDDDWDDWDDDDD......",
        ".....DDDDWDDWDDDDD......",
        ".....DDDDDDDDDDDDD......",
        ".....DDDDDRRDDDDDD......",
        "......DDDDDDDDDDD.......",
        ".......DDDDDDDDD........",
        "........DDDDDDD.........",
        "......DDDDDDDDDDD.......",
        ".....DDDDDDDDDDDDD......",
        ".....DDDDDDDDDDDDD......",
        ".....DDDDDDDDDDDDDD.....",
        ".....DDDDDDDDDDDDD......",
        ".....DDDDDDDDDDDDD......",
        "......DDDDDDDDDDD.......",
        "......DDD......DDD......",
        "......DDD......DDD......",
        ".....DDDD......DDDD.....",
        "........................"
    };

    static Texture2D CreateArtTexture(string[] art, string path)
    {
        int height = art.Length;
        int width = art[0].Length;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color[] colors = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            string line = art[height - 1 - y];
            for (int x = 0; x < width; x++)
            {
                char c = (x < line.Length) ? line[x] : '.';
                GemColor gc = c switch
                {
                    'R' => GemColor.Red,
                    'B' => GemColor.Blue,
                    'G' => GemColor.Green,
                    'Y' => GemColor.Yellow,
                    'P' => GemColor.Purple,
                    'O' => GemColor.Orange,
                    'C' => GemColor.Cyan,
                    'K' => GemColor.Pink,
                    'W' => GemColor.White,
                    'D' => GemColor.Black,
                    'N' => GemColor.Brown,
                    'L' => GemColor.LightBlue,
                    _ => GemColor.None
                };

                colors[y * width + x] = gc != GemColor.None 
                    ? GemColorPalette.GetColor(gc) 
                    : Color.clear;
            }
        }

        tex.SetPixels(colors);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static StageData CreateStageFromArt(int number, string name, string[] art, float timeLimit)
    {
        int artRows = art.Length;
        int artCols = art[0].Length;

        string dir = "Assets/Textures/Stages";
        Directory.CreateDirectory(dir);
        string texPath = $"{dir}/Stage_{number:000}_Art.png";
        Texture2D tex = CreateArtTexture(art, texPath);

        string stagePath = $"Assets/ScriptableObjects/Stages/Stage_{number:000}.asset";
        Directory.CreateDirectory(Path.GetDirectoryName(stagePath)!);

        if (AssetDatabase.LoadAssetAtPath<StageData>(stagePath) != null)
            AssetDatabase.DeleteAsset(stagePath);

        var stageData = ScriptableObject.CreateInstance<StageData>();
        stageData.stageNumber = number;
        stageData.stageName = name;
        stageData.mainRows = artRows;
        stageData.mainCols = artCols;
        stageData.paletteRows = 2;
        stageData.paletteCols = 14;
        stageData.timeLimitSeconds = timeLimit;
        stageData.pixelArtTexture = tex;

        stageData.goalLayout = new List<StageData.CellColorDef>();
        stageData.initialMainCells = new List<StageData.CellColorDef>();

        List<StageData.CellColorDef> nonVoidCells = new List<StageData.CellColorDef>();

        for (int y = 0; y < artRows; y++)
        {
            string line = art[y];
            for (int x = 0; x < artCols; x++)
            {
                char c = (x < line.Length) ? line[x] : '.';
                GemColor gc = c switch
                {
                    'R' => GemColor.Red,
                    'B' => GemColor.Blue,
                    'G' => GemColor.Green,
                    'Y' => GemColor.Yellow,
                    'P' => GemColor.Purple,
                    'O' => GemColor.Orange,
                    'C' => GemColor.Cyan,
                    'K' => GemColor.Pink,
                    'W' => GemColor.White,
                    'D' => GemColor.Black,
                    'N' => GemColor.Brown,
                    'L' => GemColor.LightBlue,
                    _ => GemColor.None
                };

                if (gc != GemColor.None)
                {
                    int row = y;
                    int col = x;
                    stageData.goalLayout.Add(new StageData.CellColorDef { row = row, col = col, color = gc });
                    nonVoidCells.Add(new StageData.CellColorDef { row = row, col = col, color = gc });
                }
            }
        }

        List<GemColor> colors = new List<GemColor>();
        foreach (var cell in nonVoidCells)
        {
            colors.Add(cell.color);
        }

        var rng = new System.Random(number * 100);
        int n = colors.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            GemColor val = colors[k];
            colors[k] = colors[n];
            colors[n] = val;
        }

        for (int i = 0; i < nonVoidCells.Count; i++)
        {
            stageData.initialMainCells.Add(new StageData.CellColorDef
            {
                row = nonVoidCells[i].row,
                col = nonVoidCells[i].col,
                color = colors[i]
            });
        }

        stageData.initialPaletteCells = new List<StageData.CellColorDef>();

        AssetDatabase.CreateAsset(stageData, stagePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return stageData;
    }

    // 16x16の幾何学的ピクセルアートをステージ番号に応じて自動生成する
    private static string[] GenerateProceduralArt(int stageNumber, out string name)
    {
        int pattern = stageNumber % 8;
        string[] art = new string[16];
        char[] colors = { 'Y', 'R', 'B', 'G', 'O', 'C', 'K', 'P' };
        
        char c1 = colors[(stageNumber) % colors.Length];
        char c2 = colors[(stageNumber + 2) % colors.Length];
        char c3 = colors[(stageNumber + 5) % colors.Length];

        switch (pattern)
        {
            case 0:
                name = "Circle";
                for (int r = 0; r < 16; r++)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int c = 0; c < 16; c++)
                    {
                        float dist = Vector2.Distance(new Vector2(r, c), new Vector2(7.5f, 7.5f));
                        if (dist < 7.5f)
                        {
                            if (dist < 3f) sb.Append(c1);
                            else if (dist < 5.5f) sb.Append(c2);
                            else sb.Append(c3);
                        }
                        else sb.Append('.');
                    }
                    art[r] = sb.ToString();
                }
                break;
            case 1:
                name = "Diamond";
                for (int r = 0; r < 16; r++)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int c = 0; c < 16; c++)
                    {
                        int dist = Mathf.Abs(r - 8) + Mathf.Abs(c - 8);
                        if (dist <= 7)
                        {
                            if (dist <= 2) sb.Append(c1);
                            else if (dist <= 5) sb.Append(c2);
                            else sb.Append(c3);
                        }
                        else sb.Append('.');
                    }
                    art[r] = sb.ToString();
                }
                break;
            case 2:
                name = "Square";
                for (int r = 0; r < 16; r++)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int c = 0; c < 16; c++)
                    {
                        if (r >= 2 && r < 14 && c >= 2 && c < 14)
                        {
                            int dist = Mathf.Max(Mathf.Abs(r - 8), Mathf.Abs(c - 8));
                            if (dist <= 2) sb.Append(c1);
                            else if (dist <= 4) sb.Append(c2);
                            else sb.Append(c3);
                        }
                        else sb.Append('.');
                    }
                    art[r] = sb.ToString();
                }
                break;
            case 3:
                name = "Cross";
                for (int r = 0; r < 16; r++)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int c = 0; c < 16; c++)
                    {
                        bool isMainCross = Mathf.Abs(r - 8) <= 1 || Mathf.Abs(c - 8) <= 1;
                        bool isDiagCross = Mathf.Abs(r - c) <= 1 || Mathf.Abs(r + c - 15) <= 1;
                        
                        if ((isMainCross || isDiagCross) && (r >= 1 && r < 15 && c >= 1 && c < 15))
                        {
                            if (isMainCross && isDiagCross) sb.Append(c1);
                            else if (isMainCross) sb.Append(c2);
                            else sb.Append(c3);
                        }
                        else sb.Append('.');
                    }
                    art[r] = sb.ToString();
                }
                break;
            case 4:
                name = "Spiral";
                for (int r = 0; r < 16; r++)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int c = 0; c < 16; c++)
                    {
                        float dist = Vector2.Distance(new Vector2(r, c), new Vector2(7.5f, 7.5f));
                        if (dist < 7.5f)
                        {
                            int angle = Mathf.FloorToInt(Mathf.Atan2(r - 7.5f, c - 7.5f) * Mathf.Rad2Deg + 180f);
                            int zone = (angle / 60) % 3;
                            if (zone == 0) sb.Append(c1);
                            else if (zone == 1) sb.Append(c2);
                            else sb.Append(c3);
                        }
                        else sb.Append('.');
                    }
                    art[r] = sb.ToString();
                }
                break;
            case 5:
                name = "Stripe";
                for (int r = 0; r < 16; r++)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int c = 0; c < 16; c++)
                    {
                        float dist = Vector2.Distance(new Vector2(r, c), new Vector2(7.5f, 7.5f));
                        if (dist < 7.5f)
                        {
                            int val = (r + c) % 4;
                            if (val == 0) sb.Append(c1);
                            else if (val == 2) sb.Append(c2);
                            else sb.Append(c3);
                        }
                        else sb.Append('.');
                    }
                    art[r] = sb.ToString();
                }
                break;
            case 6:
                name = "StarShape";
                for (int r = 0; r < 16; r++)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int c = 0; c < 16; c++)
                    {
                        float dx = Mathf.Abs(c - 7.5f);
                        float dy = Mathf.Abs(r - 7.5f);
                        if (dx + dy < 7.5f || (dx < 1.5f && dy < 7.5f) || (dy < 1.5f && dx < 7.5f))
                        {
                            if (dx + dy < 3.5f) sb.Append(c1);
                            else if (dx + dy < 6f) sb.Append(c2);
                            else sb.Append(c3);
                        }
                        else sb.Append('.');
                    }
                    art[r] = sb.ToString();
                }
                break;
            default:
                name = "GridPattern";
                for (int r = 0; r < 16; r++)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int c = 0; c < 16; c++)
                    {
                        float dist = Vector2.Distance(new Vector2(r, c), new Vector2(7.5f, 7.5f));
                        if (dist < 7.5f)
                        {
                            bool isEven = (r / 2 + c / 2) % 2 == 0;
                            if (dist < 4f) sb.Append(isEven ? c1 : c2);
                            else sb.Append(isEven ? c2 : c3);
                        }
                        else sb.Append('.');
                    }
                    art[r] = sb.ToString();
                }
                break;
        }

        return art;
    }

    [MenuItem("Tools/SortGems/Create Game Scene")]
    public static void CreateScene()
    {
        // GoogleMobileAdsSettings.asset が存在しない場合は、ダミーのAppIDを設定したアセットを自動生成してパッケージのエラーを防ぐ
        string settingsDir = Path.Combine(Application.dataPath, "GoogleMobileAds/Resources");
        string settingsPath = Path.Combine(settingsDir, "GoogleMobileAdsSettings.asset");
        if (!File.Exists(settingsPath))
        {
            Directory.CreateDirectory(settingsDir);
            string yaml = @"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: a187246822bbb47529482707f3e0eff8, type: 3}
  m_Name: GoogleMobileAdsSettings
  m_EditorClassIdentifier: 
  adMobAndroidAppId: ca-app-pub-3940256099942544~3347511713
  adMobIOSAppId: ca-app-pub-3940256099942544~1458002511
  enableKotlinXCoroutinesPackagingOption: 1
  optimizeInitialization: 0
  optimizeAdLoading: 0
  userTrackingUsageDescription: 
  validateGradleDependencies: 1
";
            File.WriteAllText(settingsPath, yaml);
            AssetDatabase.Refresh();
        }

        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(activeScene.path))
        {
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
        }

        // ---- 1. シーン ----
        var scenePath = "Assets/Scenes/GameScene.unity";
        Directory.CreateDirectory(Path.GetDirectoryName(scenePath)!);
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
            UnityEditor.SceneManagement.NewSceneMode.Single);

        // カメラ
        var cam = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (cam) { cam.orthographic = true; cam.orthographicSize = 5f; }

        // ---- 2. Managers ----
        var mgr        = new GameObject("[Managers]");
        var gmObj      = Child(mgr, "GameManager");  var gm  = gmObj.AddComponent<GameManager>();
        var gridMgrObj = Child(mgr, "GridManager");  var grd = gridMgrObj.AddComponent<GridManager>();
        var adObj      = Child(mgr, "AdManager");    adObj.AddComponent<AdManager>();
        var sndObj     = Child(mgr, "SoundManager"); var snd = sndObj.AddComponent<SoundManager>();

        // Assets/Sound/BGM 以下の mp3 ファイルを自動ロード
        var bgmClipsList = new List<AudioClip>();
        string bgmDir = Path.Combine(Application.dataPath, "Sound/BGM");
        if (Directory.Exists(bgmDir))
        {
            string[] bgmFiles = Directory.GetFiles(bgmDir, "*.mp3", SearchOption.AllDirectories);
            foreach (var bgmFile in bgmFiles)
            {
                string relativePath = "Assets" + bgmFile.Substring(Application.dataPath.Length).Replace('\\', '/');
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(relativePath);
                if (clip != null) bgmClipsList.Add(clip);
            }
        }

        // SoundManager の _bgmClips にシリアライズしてアサイン
        var sndSo = new SerializedObject(snd);
        var bgmClipsProp = sndSo.FindProperty("_bgmClips");
        bgmClipsProp.ClearArray();
        for (int i = 0; i < bgmClipsList.Count; i++)
        {
            bgmClipsProp.InsertArrayElementAtIndex(i);
            bgmClipsProp.GetArrayElementAtIndex(i).objectReferenceValue = bgmClipsList[i];
        }
        sndSo.ApplyModifiedProperties();

        SetRef(gm, "_gridManager", grd);

        // ---- 3. EventSystem ----
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        // ---- 4. Canvas (uGUI — パズルグリッド描画専用) ----
        var canvasObj = new GameObject("[Canvas]");
        var canvas    = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(REF_W, REF_H);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // ---- 5. GamePlayPanel (uGUI — グリッドとパレットのみ) ----
        var gamePlayPanel = MakeFullPanel("GamePlayPanel", canvasObj.transform, new Color(0.15f, 0.15f, 0.18f, 0f));
        gamePlayPanel.SetActive(false);

        // ---- 6. Main Grid ----
        float gridPx  = MAIN_COLS * (CELL_SIZE + CELL_SPACING);
        float gridH   = MAIN_ROWS * (CELL_SIZE + CELL_SPACING);

        float palH = PAL_ROWS * (CELL_SIZE + CELL_SPACING);
        float contentH = gridH + PALETTE_GAP + palH;
        float remainingH = (REF_H - HEADER_H - BUTTON_H) - contentH;
        float topMargin = remainingH / 2f;
        float gridTop = HEADER_H + topMargin;

        var gridContainerObj = MakeRect("GridContainer", gamePlayPanel.transform,
            ancMin: new Vector2(0.5f,1), ancMax: new Vector2(0.5f,1),
            pivot: new Vector2(0.5f,1),
            pos: new Vector2(0, -gridTop),
            size: new Vector2(gridPx, gridH));

        var gridView   = gridContainerObj.gameObject.AddComponent<GridView>();
        var mainLayout = gridContainerObj.gameObject.AddComponent<GridLayoutGroup>();
        ConfigureLayout(mainLayout, MAIN_COLS, CELL_SIZE, CELL_SPACING);

        // ---- 7. Palette ----
        float palW = PAL_COLS * (CELL_SIZE + CELL_SPACING);
        float palTop = gridTop + gridH + PALETTE_GAP;

        var paletteObj = MakeRect("PaletteContainer", gamePlayPanel.transform,
            ancMin: new Vector2(0.5f,1), ancMax: new Vector2(0.5f,1),
            pivot: new Vector2(0.5f,1),
            pos: new Vector2(0, -palTop),
            size: new Vector2(palW, palH));

        var paletteLayout = paletteObj.gameObject.AddComponent<GridLayoutGroup>();
        ConfigureLayout(paletteLayout, PAL_COLS, CELL_SIZE, CELL_SPACING);

        // GridView 参照
        SetRef(gridView, "_gridManager",   grd);
        SetRef(gridView, "_mainLayout",    mainLayout);
        SetRef(gridView, "_paletteLayout", paletteLayout);

        // ---- 8. GemCell Prefab ----
        var prefabPath = "Assets/Prefabs/GemCell.prefab";
        Directory.CreateDirectory(Path.GetDirectoryName(prefabPath)!);

        var cellRoot = new GameObject("GemCell");
        var cellView = cellRoot.AddComponent<GemCellView>();
        cellRoot.AddComponent<RectTransform>().sizeDelta = new Vector2(CELL_SIZE, CELL_SIZE);

        var aspect = cellRoot.AddComponent<AspectRatioFitter>();
        aspect.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
        aspect.aspectRatio = 1.0f;

        var bgImg  = MakeImageChild("BackgroundImage", cellRoot.transform,
                        new Color(0.18f, 0.18f, 0.22f, 1f), Vector2.zero, Vector2.one);
        var socketImg = MakeImageChild("SocketImage", cellRoot.transform,
                        new Color(0f, 0f, 0f, 0.55f), Vector2.zero, Vector2.one);
        var gemImg = MakeImageChild("GemImage", cellRoot.transform,
                        Color.white, Vector2.zero, Vector2.one);
        
        var bevelShadow = MakeImageChild("BevelShadow", gemImg.transform,
                            Color.white, Vector2.zero, Vector2.one);
        bevelShadow.sprite = GemColorPalette.BevelShadowSprite;

        var shadowEff = gemImg.gameObject.AddComponent<Shadow>();
        shadowEff.effectColor = new Color(0f, 0f, 0f, 0.48f);
        shadowEff.effectDistance = new Vector2(2f, -2f);

        var mark   = MakeImageChild("CompletedMark", cellRoot.transform,
                        new Color(1f, 1f, 1f, 0.4f), new Vector2(0.15f, 0.55f), new Vector2(0.85f, 0.85f));
        mark.sprite = GemColorPalette.RoundedRectSprite;
        mark.gameObject.SetActive(false);

        SetRef(cellView, "_gemImage",       gemImg);
        SetRef(cellView, "_backgroundImage",bgImg);
        SetRef(cellView, "_socketImage",    socketImg);
        SetRef(cellView, "_completedMark",  mark.gameObject);

        var trigger = cellRoot.AddComponent<EventTrigger>();
        var entry   = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(entry.callback, cellView.OnPointerClick);
        trigger.triggers.Add(entry);

        var gemCellPrefab = PrefabUtility.SaveAsPrefabAsset(cellRoot, prefabPath);
        Object.DestroyImmediate(cellRoot);

        SetRef(gridView, "_cellPrefab", gemCellPrefab.GetComponent<GemCellView>());
        new SerializedObject(gridView).ApplyModifiedProperties();

        // ---- 9. ステージアセット生成 ----
        var handcraftedArts = new (string name, string[] art, float time)[]
        {
            // Easy (1-2色, 少ピクセル)
            ("Star",        StarArt,        120f),
            ("Music Note",  MusicNoteArt,   120f),
            ("Heart",       HeartArt,       150f),
            ("Umbrella",    UmbrellaArt,    150f),
            ("Moon & Star", MoonStarArt,    150f),
            ("Crown",       CrownArt,       150f),
            ("Diamond",     DiamondArt,     180f),
            ("Sun",         SunArt,         180f),
            // Normal (2-3色, 中ピクセル)
            ("Tree",        TreeArt,        180f),
            ("Sunflower",   FlowerArt,      180f),
            ("Apple",       AppleArt,       200f),
            ("Turtle",      TurtleArt,      200f),
            ("Butterfly",   ButterflyArt,   200f),
            ("Dog",         DogArt,         200f),
            ("Cat",         CatArt,         220f),
            ("Ghost",       GhostArt,       220f),
            ("Smiley",      SmileyArt,      220f),
            ("Fish",        FishArt,        220f),
            ("Snail",       SnailArt,       220f),
            ("Sailboat",    SailboatArt,    240f),
            // Hard (3-6色, 多ピクセル)
            ("Mushroom",    MushroomArt,    240f),
            ("Frog",        FrogArt,        240f),
            ("Cupcake",     CupcakeArt,     240f),
            ("Car",         CarArt,         260f),
            ("Cake",        CakeArt,        260f),
            ("Penguin",     PenguinArt,     260f),
            ("Rocket",      RocketArt,      280f),
            ("House",       HouseArt,       280f),
            ("Watermelon",  WatermelonArt,  280f),
            ("Rainbow",     RainbowArt,     300f),
            // 18x18 ステージ
            ("Panda",       PandaArt18,     300f),
            ("Whale",       WhaleArt18,     300f),
            ("Donut",       DonutArt18,     280f),
            // 24x24 ステージ
            ("Owl",         OwlArt24,       360f),
            ("Sailboat L",  SailboatArt24,  360f),
            ("Robot",       RobotArt24,     360f),
        };

        List<StageData> stages = new List<StageData>();
        for (int i = 0; i < handcraftedArts.Length; i++)
        {
            var (name, art, time) = handcraftedArts[i];
            stages.Add(CreateStageFromArt(i + 1, name, art, time));
        }

        for (int i = handcraftedArts.Length + 1; i <= 100; i++)
        {
            string stageName;
            string[] art = GenerateProceduralArt(i, out stageName);
            stages.Add(CreateStageFromArt(i, stageName, art, 300f));
        }

        // ---- 10. UI Toolkit (ScreenManager) ----
        string panelSettingsPath = "Assets/UI/DefaultPanelSettings.asset";
        var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelSettingsPath);
        if (panelSettings == null)
            Debug.LogError("[CreateGameScene] PanelSettings not found at " + panelSettingsPath);

        var titleUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/TitleScreen.uxml");
        var stageSelectUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/StageSelectScreen.uxml");
        var gamePlayUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/GamePlayHUD.uxml");

        if (titleUxml == null || stageSelectUxml == null || gamePlayUxml == null)
            Debug.LogError("[CreateGameScene] UXML files not found under Assets/UI/Screens/");

        var uiToolkitObj = new GameObject("[UIToolkit]");
        var uiDoc = uiToolkitObj.AddComponent<UIDocument>();
        if (panelSettings != null)
            uiDoc.panelSettings = panelSettings;

        var screenMgr = uiToolkitObj.AddComponent<ScreenManager>();

        var smSo = new SerializedObject(screenMgr);
        smSo.FindProperty("_titleScreen").objectReferenceValue = titleUxml;
        smSo.FindProperty("_stageSelectScreen").objectReferenceValue = stageSelectUxml;
        smSo.FindProperty("_gamePlayHUD").objectReferenceValue = gamePlayUxml;
        smSo.FindProperty("_gameManager").objectReferenceValue = gm;
        smSo.FindProperty("_gridView").objectReferenceValue = gridView;
        smSo.FindProperty("_gridManager").objectReferenceValue = grd;
        smSo.FindProperty("_uguiGamePlayPanel").objectReferenceValue = gamePlayPanel;
        smSo.FindProperty("_cellPrefab").objectReferenceValue = gemCellPrefab.GetComponent<GemCellView>();

        var smStagesProp = smSo.FindProperty("_stages");
        smStagesProp.ClearArray();
        for (int i = 0; i < stages.Count; i++)
        {
            smStagesProp.InsertArrayElementAtIndex(i);
            smStagesProp.GetArrayElementAtIndex(i).objectReferenceValue = stages[i];
        }
        smSo.ApplyModifiedProperties();

        // ---- 11. Audio Visualizer シングルトンシステムの自動生成 ----
        // 画面最下部に固定され、シーン切り替え時も常駐する Canvas
        var visualizerCanvasObj = new GameObject("[DontDestroyVisualizer]");
        var vizCanvas = visualizerCanvasObj.AddComponent<Canvas>();
        vizCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        vizCanvas.sortingOrder = 999; // 他の全UIの最前面に描画

        var vizScaler = visualizerCanvasObj.AddComponent<CanvasScaler>();
        vizScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        vizScaler.referenceResolution = new Vector2(REF_W, REF_H);
        vizScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        vizScaler.matchWidthOrHeight = 0f;

        visualizerCanvasObj.AddComponent<GraphicRaycaster>(); // UIイベント遮断防止のため、子要素のRaycastTargetはOFFになります

        // バーを配置する親コンテナ (RectTransform)
        // 画面の最下部（Bottom）にアンカーを固定
        var containerRt = MakeRect("VisualizerContainer", visualizerCanvasObj.transform,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), 
            new Vector2(0f, 0f), new Vector2(0f, 220f)); // 画面幅いっぱいにストレッチ、高さ220f

        // マネージャーの追加
        var vizManager = visualizerCanvasObj.AddComponent<VisualizerManager>();

        // インスペクター値を SerializedObject からセット
        var vizSo = new SerializedObject(vizManager);
        vizSo.FindProperty("_container").objectReferenceValue = containerRt;
        vizSo.FindProperty("_barCount").intValue = 32; // 32本のバー
        vizSo.FindProperty("_spacing").floatValue = 2f; // 間隔2px
        vizSo.FindProperty("_sensitivity").floatValue = 2500f; // 感度
        vizSo.FindProperty("_lerpSpeed").floatValue = 12f; // スムーズさ
        vizSo.FindProperty("_barColor").colorValue = new Color(1f, 1f, 1f, 0.45f); // 白色、透明度 0.45f
        vizSo.FindProperty("_minHeight").floatValue = 10f;
        vizSo.FindProperty("_maxHeight").floatValue = 200f;
        vizSo.ApplyModifiedProperties();

        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SortGems] GameScene 構築完了 — UI Toolkit (ScreenManager) + uGUIパズルグリッド + ピクセルアート30ステージ + BGM自動割当 + ビジュアライザー生成");
    }

    // ===== ヘルパー =====

    static void ConfigureLayout(GridLayoutGroup layout, int cols, float cellSize, float spacing)
    {
        layout.constraint       = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount  = cols;
        layout.cellSize         = new Vector2(cellSize, cellSize);
        layout.spacing          = new Vector2(spacing, spacing);
        layout.padding          = new RectOffset(0, 0, 0, 0);
    }

    static GameObject Child(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        return go;
    }

    static RectTransform MakeRect(string name, Transform parent,
        Vector2 ancMin, Vector2 ancMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin        = ancMin;
        rect.anchorMax        = ancMax;
        rect.pivot            = pivot;
        rect.anchoredPosition = pos;
        rect.sizeDelta        = size;
        return rect;
    }

    static Text MakeText(string name, Transform parent, string content, int fontSize,
        Vector2 ancMin, Vector2 ancMax, Vector2 pos, Vector2 size)
    {
        var rt   = MakeRect(name, parent, ancMin, ancMax, new Vector2(0.5f,0.5f), pos, size);
        var text = rt.gameObject.AddComponent<Text>();
        text.text      = content;
        text.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize  = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color     = Color.white;
        return text;
    }

    static (UnityEngine.UI.Slider slider, UnityEngine.UI.Image fill) MakeSlider(string name, Transform parent)
    {
        var rt   = MakeRect(name, parent,
            new Vector2(0,0.5f), new Vector2(1,0.5f),
            new Vector2(0.5f,0.5f), new Vector2(0,10), new Vector2(-20,24));
        var slider = rt.gameObject.AddComponent<UnityEngine.UI.Slider>();

        var bg    = MakeImageChild("Background", rt, new Color(0.25f,0.25f,0.25f,1), Vector2.zero, Vector2.one);
        var area  = MakeRect("Fill Area", rt, Vector2.zero, Vector2.one, new Vector2(0,0.5f), Vector2.zero, Vector2.zero);
        var fill  = MakeImageChild("Fill", area, Color.green, Vector2.zero, Vector2.one);

        slider.fillRect     = fill.GetComponent<RectTransform>();
        slider.targetGraphic = fill;
        slider.value         = 1f;
        return (slider, fill);
    }

    static UnityEngine.UI.Image MakeImageChild(string name, Transform parent,
        Color color, Vector2 ancMin, Vector2 ancMax)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<UnityEngine.UI.Image>();
        img.color = color;
        var r   = img.GetComponent<RectTransform>();
        r.anchorMin = ancMin; r.anchorMax = ancMax;
        r.sizeDelta = Vector2.zero;
        return img;
    }

    static GameObject MakeFullPanel(string name, Transform parent, Color bg)
    {
        var rt  = MakeRect(name, parent, Vector2.zero, Vector2.one,
                           new Vector2(0.5f,0.5f), Vector2.zero, Vector2.zero);
        rt.gameObject.AddComponent<UnityEngine.UI.Image>().color = bg;
        rt.gameObject.SetActive(false);
        return rt.gameObject;
    }

    static UnityEngine.UI.Button CreateButton(string name, string label, Transform parent, Vector2 pos, Vector2 size)
    {
        var rt  = MakeRect(name, parent,
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(0.5f,0.5f), pos, size);
        var img = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.28f, 0.28f, 0.38f, 1f);
        var btn = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = img;

        MakeText("Text", rt, label, 22,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return btn;
    }

    // SerializedObject への参照セット（型を自動判別）
    static void SetRef<T>(Object target, string propName, T value) where T : Object
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(propName);
        if (prop == null)
        {
            Debug.LogError($"[CreateGameScene] Property '{propName}' not found on {target.name}");
            return;
        }
        prop.objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }
}
