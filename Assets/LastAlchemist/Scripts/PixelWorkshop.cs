using System.Collections.Generic;
using UnityEngine;

namespace LastAlchemist
{
    // Small original code-native pixel sprites; no external art or downloads required.
    public sealed class PixelWorkshop : System.IDisposable
    {
        readonly List<Object> assets = new List<Object>();
        readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        readonly Dictionary<char, Color> palette = new Dictionary<char, Color>
        {
            ['o'] = C("19292d"), ['k'] = C("293d40"), ['w'] = C("f6e6bc"),
            ['g'] = C("59bfa0"), ['G'] = C("b4e6b3"), ['t'] = C("337c6b"),
            ['b'] = C("497a89"), ['B'] = C("9bd3d1"), ['r'] = C("e57b48"),
            ['R'] = C("ffc879"), ['y'] = C("d5ac68"), ['s'] = C("96744e"),
            ['d'] = C("5e503c"), ['p'] = C("877798")
        };
        Sprite square;
        public static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }

        public PixelWorkshop()
        {
            var texture = new Texture2D(1, 1); texture.SetPixel(0, 0, Color.white); texture.Apply();
            square = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
            assets.Add(texture); assets.Add(square);
            Add("player", new[] {
                ".......oo.......", "......oppo......", ".....oppppo.....", "....oppppppo....",
                "...oppppppppo...", "..oyyyyyyyyyyo..", ".....owwwo......", ".....owowo......",
                ".....owwwo......", "....otGGgto.....", "...otgGGggto....", "...owgggggwo....",
                "....ogggggo.....", "....oyyyyyo.....", "....odoodo......", "...oddo.oddo...." });
            Add("slime", new[] {
                "................", "................", "................", "................",
                "......GGG.......", "....GGGGGgg.....", "...GGGGGgggg....", "..GGGGGgggggg...",
                "..GwwGggwwggg...", ".gGwokggwokggg..", ".gGGgggggggggg..", ".ggggggggggggg..",
                "..ggtggggtggg...", "...ttttttttt....", "................", "................" });
            Add("herb", new[] {
                ".......R........", "......RRr.......", ".....RRRrr......", "....rRRrrrr.....",
                ".....rrRrr......", "......rrr.......", ".......t........", "...Gg..t..Gg....",
                "....Gg.t.Ggg....", ".....GgtGgg.....", "......gtgg......", ".......t........",
                "...ddddtdddd....", "....sssssss.....", ".....ddddd......", "................" });
            Add("mushroom", new[] {
                "................", "................", "......pppp......", "....ppPPpppp....",
                "...pPPPPPPppp...", "..pPPwwPPwwppp..", "..ppPPPPPPpppp..", "....oooooooo....",
                "......owwo......", "......owwo......", "......owwo......", ".....oowwoo.....",
                "....dddddddd....", "...dssssssssdd..", "................", "................" });
            Add("crystal", new[] {
                ".......B........", "......BBB.......", ".....BBbBB......", "....BBbbbBB.....",
                "...BBbbbbbBB....", "..BBbbbbbbbBB...", "...BbbbbbbbB....", "...BbbbbbbbB....",
                "....BbbbbbB.....", "....BbbbbbB.....", ".....BbbbbB.....", "..BBBBbbbbBBBB..",
                ".BBBBBBBBBBBBBB..", "..bbbbbbbbbbbb...", "....dddddddd....", "................" });
            Add("cauldron", new[] {
                "......B..B......", "...B......B.....", ".....B..B.......", "................",
                "..yyyyyyyyyyyy..", ".okggGGGGggggko.", ".okkkkkkkkkkkko.", "..kbbbbbbbbbbk..",
                "..kbbbbbbbbbbk..", "..kbbbbbyybbbk..", "...kbbbbyybbk...", "....kkkkkkkk....",
                "....d.rRRr.d....", "...ddrrRRrrdd...", "..dddddddddddd..", "................" });
            Add("market", new[] {
                "..oooooooooooo..", ".oyyyywwyyyywwo.", "oyyyyywwyyyywwyo", "oyyyyywwyyyywwyo",
                "..s..........s..", "..s..........s..", "..s..B...R...s..", "..s.BBB.RRR..s..",
                "..s.BBB.RRR..s..", ".oyyyyyyyyyyyyo.", ".osssssssssssso.", ".ossssyyyssssso.",
                ".ossssywyssssso.", ".ossssyyyssssso.", "..dd........dd..", "................" });
            Add("upgrade", new[] {
                "......yyyy......", "...y..ywwy..y...", "..yyy.ywwy.yyy..", "...yyyyyyyyyy...",
                "..yyyyooooyyyy..", "yyyyoobbbbooyyyy", "ywwyobBbbBboywwy", "ywwyobbbbbboywwy",
                "yyyyoobbbbooyyyy", "..yyyyooooyyyy..", "...yyyyyyyyyy...", "..yyy.ywwy.yyy..",
                "...y..yyyy..y...", "....ssssssss....", "...dddddddddd...", "................" });
            Add("turret", new[] {
                "............R...", "..........RRR...", "....yyyyyRRR....", "...ywwwwyRR.....",
                "..yyyyyyyyy.....", "....oyyyyo......", "....obbbbo......", "...oobbbboo.....",
                "...okbbbbko.....", "...okbbbbko.....", "....okkkko......", "....oyyyyo......",
                "...oyyyyyyo.....", "..oddddddddo....", ".oddddddddddo...", "................" });
            Add("wretch", new[] {
                ".......oo.......", "......okko......", ".....okrrko.....", ".....okkkko.....",
                "......oooo......", ".....okkkko.....", "....okkkkkko....", "...okkkkkkkko...",
                "...okkkkkkkko...", "....okkkkkko....", ".....okkkko.....", ".....okokko.....",
                "....oo..okoo....", "...oo....okoo...", "................", "................" });
            Add("brute", new[] {
                ".....oooooo.....", "....okkkkkko....", "...okrrkkrrko...", "...okkkkkkkko...",
                "..ookkkkkkkkoo..", ".okkkkkkkkkkkko..", "okkkkkkkkkkkkkko.", "okkkkkkkkkkkkkko.",
                ".okkkkkkkkkkkko..", "..okkkkkkkkkko...", "...okkkkkkkko...", "...ookkkkkkoo...",
                "..ooookkooooko..", ".ooo..okko..ooo..", "......oooo......", "................" });
            Add("boss", new[] {
                "..R..........R..", "..RR...oo...RR..", "...RR.okko.RR...", "....RokrrkoR....",
                "...ookkkkkkoo...", "..okkkkkkkkkko..", ".okkkkkkkkkkkkko.", "okkkkkkkkkkkkkko",
                "okkkkkkkkkkkkkko", ".okkkkkkkkkkkkko.", "..okkkkkkkkkko..", "...okkkkkkkko...",
                "..ookkkookkkkoo.", ".oooookoookooooo.", "oo...oo..oo...oo", "................" });
            Add("tree", new[] {
                ".......gg.......", "......gGGg......", ".....gGGGgg.....", "....ggGGgggg....",
                "...ggGGGggggg...", "..tggGGGgggggt..", "....tggggggt....", "...ggGGgggggg...",
                "..tggGGggggggt..", ".ttggGgggggggtt.", "....tttttttt....", "......sssd......",
                "......sssd......", "......sssd......", ".....dddddd.....", "................" });
            Add("bottle", new[] {
                "......yyyy......", "......BBBB......", "......BwwB......", ".....BBwwBB.....",
                "....BBwwwwBB....", "....BwwwwwwB....", "...BBwRRrrrBB...", "...BwRRrrrrrB...",
                "...BwRRrrrrrB...", "...BwRrrrrrrB...", "...BwrrrrrrrB...", "...BBrrrrrrBB...",
                "....BBBBBBBB....", "................", "................", "................" });
        }

        void Add(string name, string[] rows)
        {
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color[256];
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                    pixels[(15 - y) * 16 + x] = x < rows[y].Length && palette.TryGetValue(rows[y][x], out var c) ? c : Color.clear;
            tex.SetPixels(pixels); tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(.5f, .2f), 16);
            sprites.Add(name, sprite); assets.Add(tex); assets.Add(sprite);
        }

        public SpriteRenderer Block(Transform parent, string name, Vector2 pos, Vector2 size, string color, int order)
        {
            var renderer = Make(parent, name, pos, square, order);
            renderer.transform.localScale = new Vector3(size.x, size.y, 1);
            renderer.color = C(color);
            return renderer;
        }

        public SpriteRenderer Actor(Transform parent, string sprite, Vector2 pos, float scale = 1.6f)
        {
            var renderer = Make(parent, sprite, pos, sprites[sprite], 100 - Mathf.RoundToInt(pos.y * 10));
            renderer.transform.localScale = Vector3.one * scale;
            return renderer;
        }

        public Sprite CreateResourceSprite(Texture2D texture, float worldHeight, Vector2 pivot)
        {
            if (!texture) return null;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, texture.height / worldHeight);
            assets.Add(sprite);
            return sprite;
        }

        SpriteRenderer Make(Transform parent, string name, Vector2 pos, Sprite sprite, int order)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = pos;
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = order;
            return renderer;
        }

        public void BuildGround(Transform root)
        {
            Texture2D[] backgrounds = {
                Resources.Load<Texture2D>("Art/alchemy-workshop-left"),
                Resources.Load<Texture2D>("Art/alchemy-workshop-bg"),
                Resources.Load<Texture2D>("Art/alchemy-workshop-right")
            };
            if (backgrounds[0] && backgrounds[1] && backgrounds[2])
            {
                float height = 12.7f;
                float panelWidth = backgrounds[1].width / (backgrounds[1].height / height);
                string[] names = { "Bioluminescent greenhouse", "Painted alchemy workshop", "Fortified city gate" };
                for (int i = 0; i < backgrounds.Length; i++)
                {
                    var texture = backgrounds[i];
                    var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), texture.height / height);
                    assets.Add(sprite);
                    var backdrop = Make(root, names[i], new Vector2((i - 1) * (panelWidth - .02f), .05f), sprite, -150);
                    backdrop.transform.localScale = new Vector3(1.004f, 1.04f, 1);
                    backdrop.color = new Color(.84f, .84f, .84f, 1);
                }
                var walkGlow = Block(root, "Continuous walkable floor glow", new Vector2(0, -3.05f), new Vector2(panelWidth * 3f, 2.1f), "172224", -120);
                walkGlow.color = new Color(.09f, .13f, .14f, .43f);
                return;
            }
            Block(root, "Island border", Vector2.zero, new Vector2(23, 11.3f), "152c2b", -110);
            Block(root, "Meadow", new Vector2(-5.7f, 0), new Vector2(11.3f, 10.8f), "2c4a3f", -100);
            Block(root, "Workshop", new Vector2(5.65f, 0), new Vector2(11.4f, 10.8f), "4d493c", -100);
            var rng = new System.Random(23);
            for (int y = -5; y <= 5; y++)
                for (int x = -11; x <= 11; x++)
                {
                    if (x >= 0)
                        Block(root, "Floor tile", new Vector2(x, y), new Vector2(.95f, .95f), (x + y) % 2 == 0 ? "555143" : "514d40", -99);
                    else
                    {
                        var p = new Vector2(x + (float)rng.NextDouble() * .6f, y + (float)rng.NextDouble() * .6f);
                        Block(root, "Grass", p, new Vector2(.08f, .15f), "45654c", -98);
                        Block(root, "Grass", p + new Vector2(.15f, -.05f), new Vector2(.06f, .1f), "45654c", -98);
                    }
                }
            Block(root, "Path", new Vector2(0, -1.1f), new Vector2(22.5f, 1.1f), "79705a", -95);
            for (int x = -11; x < 12; x++)
                Block(root, "Paving", new Vector2(x, -1.1f), new Vector2(.88f, .82f), x % 2 == 0 ? "8b8067" : "837960", -94);
            Block(root, "Workshop rug", new Vector2(4.7f, .8f), new Vector2(9.4f, 3f), "344e4c", -93);
            Block(root, "Rug inset", new Vector2(4.7f, .8f), new Vector2(9.1f, 2.7f), "3b5753", -92);
            for (int i = 0; i < 9; i++)
            {
                Actor(root, "tree", new Vector2(-10.6f + i * 2.65f, 4.2f), 2.2f);
                if (i < 3) Actor(root, "tree", new Vector2(-10.5f + i * 2.6f, -4.9f), 1.8f);
            }
            for (int i = 0; i < 4; i++)
            {
                Block(root, "Shelf", new Vector2(2.2f + i * 2.1f, 3.15f), new Vector2(1.5f, .25f), "a0855a", 60);
                Actor(root, "bottle", new Vector2(2.2f + i * 2.1f, 3.32f), .65f);
            }
        }

        public void Dispose()
        { foreach (var asset in assets) if (asset) Object.Destroy(asset); }
    }
}
