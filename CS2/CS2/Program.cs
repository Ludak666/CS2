using ClickableTransparentOverlay;
using CS2;
using Swed64;
using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using System.Runtime.InteropServices;
using Veldrid.OpenGLBinding;


namespace CS2MULTI
{

    class Program : Overlay
    {
        //imports and struct
        [DllImport("user32.dll")]

        static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int right;
            public int top;
            public int bottom;
        }

        public RECT GetWindowRect(IntPtr hWnd)
        {
            RECT rect = new RECT();
            GetWindowRect(hWnd, out rect);
            return rect;
        }

        Swed swed = new Swed("cs2");
        Offsets offsets = new Offsets();
        ImDrawListPtr drawList;

        Entity localPlayer = new Entity();
        List<Entity> entities = new List<Entity>();
        List<Entity> enemyTeam = new List<Entity>();
        List<Entity> playerTeam = new List<Entity>();

        IntPtr client;

        //global colors

        Vector4 teamColor = new Vector4(0, 0, 1, 1);//RGBA,blue teamates
        Vector4 enemyColor = new Vector4(1, 0, 0, 1);//enemies,red 
        Vector4 healthBarColor = new Vector4(0, 1, 0, 1);//green
        Vector4 healthTextColor = new Vector4(0, 0, 0, 1); //black

        //screen var

        Vector2 windowLocation = new Vector2(0, 0);
        Vector2 WindowSize = new Vector2(1920, 1080);
        Vector2 lineOrigin = new Vector2(1920 / 2, 1080);
        Vector2 windowCenter = new Vector2(1920 / 2, 1080 / 2);

        bool enableEsp = true;

        bool enableTeamLine = true;
        bool enableTeamBox = true;
        bool enableTeamDot = false;
        bool enableTeamHealthBar = true;
        bool enableTeamDistance = true;

        bool enableEnemyLine = true;
        bool enableEnemyBox = true;
        bool enableEnemyDot = false;
        bool enableEnemyHealthBar = true;
        bool enableEnemyDistance = true;

        protected override void Render()
        {
            DrawMenu();
            DrawOverlay();
            Esp();
            ImGui.End();

        }

        void Esp()
        {
            drawList = ImGui.GetBackgroundDrawList();
            if(enableEsp)
            {
                try
                {
                    foreach(var entity in entities)
                    {
                        if(entity.teamNum == localPlayer.teamNum)
                        {
                            DrawVisual(entity, teamColor, enableTeamLine, enableTeamBox, enableTeamDot, enableTeamHealthBar, enableTeamDistance);
                        }
                        else
                        {
                            DrawVisual(entity, enemyColor, enableEnemyLine, enableEnemyBox, enableEnemyDot, enableEnemyHealthBar, enableEnemyDistance);
                        }
                    }
                } catch { }
               
            }
        }
        void DrawVisual(Entity entity, Vector4 color, bool line, bool box, bool dot, bool healthbar, bool distance)
        {
            if (IsPixelInsideScreen(entity.originScreenPosition))
            {
                //convert our colors to uints
                uint uintColor = ImGui.ColorConvertFloat4ToU32(color);
                uint uintHealthTextColor = ImGui.ColorConvertFloat4ToU32(healthTextColor);
                uint uintHealthBarColor = ImGui.ColorConvertFloat4ToU32(healthBarColor);

                Vector2 boxWidth = new Vector2((entity.originScreenPosition.Y - entity.absScreenPosition.Y) / 2, 0f);
                Vector2 boxStart = Vector2.Subtract(entity.absScreenPosition, boxWidth);
                Vector2 boxEnd = Vector2.Add(entity.originScreenPosition, boxWidth);

                float barPercent = entity.health / 100f;
                Vector2 barHeigh = new Vector2(0, barPercent * (entity.originScreenPosition.Y - entity.absScreenPosition.Y));
                Vector2 barStart = Vector2.Subtract(Vector2.Subtract(entity.originScreenPosition, boxWidth), barHeigh);
                Vector2 barEnd = Vector2.Subtract(entity.originScreenPosition, Vector2.Add(boxWidth, new Vector2(-4, 0)));

                if (line)
                {
                    drawList.AddLine(lineOrigin, entity.originScreenPosition, uintColor, 3);
                }
                if(box)
                {
                    drawList.AddRect(boxStart, boxEnd, uintColor, 3);
                }
                if(dot)
                {
                    drawList.AddCircleFilled(entity.originScreenPosition, 5, uintColor);
                }
                if (healthbar)
                {
                    drawList.AddText(entity.originScreenPosition, uintHealthBarColor, $"hp: {entity.health}");
                    drawList.AddRectFilled(barStart, barEnd, uintHealthBarColor);
                }

            }
        }
        bool IsPixelInsideScreen(Vector2 pixel)
        {
            return pixel.X > windowLocation.X && pixel.X < windowLocation.X + WindowSize.X && pixel.Y > windowLocation.Y && pixel.Y < WindowSize.Y + windowLocation.Y;
        }
        ViewMatrix ReadMatrix(IntPtr matrixAddress)
        {
            var viewMatrix = new ViewMatrix();
            var floatMatrix = swed.ReadMatrix(matrixAddress);

            viewMatrix.m11 = floatMatrix[0];
            viewMatrix.m12 = floatMatrix[1];
            viewMatrix.m13 = floatMatrix[2];
            viewMatrix.m14 = floatMatrix[3];

            viewMatrix.m21 = floatMatrix[4];
            viewMatrix.m22 = floatMatrix[5];
            viewMatrix.m23 = floatMatrix[6];
            viewMatrix.m24 = floatMatrix[7];

            viewMatrix.m31 = floatMatrix[8];
            viewMatrix.m32 = floatMatrix[9];
            viewMatrix.m33 = floatMatrix[10];
            viewMatrix.m34 = floatMatrix[11];

            viewMatrix.m41 = floatMatrix[12];
            viewMatrix.m42 = floatMatrix[13];
            viewMatrix.m43 = floatMatrix[14];
            viewMatrix.m44 = floatMatrix[15];

            return viewMatrix;

        }

        Vector2 WorldToScreen(ViewMatrix matrix, Vector3 pos, int width, int height)
        {
            Vector2 screenCoordinates = new Vector2();

            //calculate screenW

            float screenW = (matrix.m41 * pos.X) + (matrix.m42 * pos.Y) + (matrix.m43 * pos.Z) + matrix.m44;

            if (screenW > 0.001f) //check entity in front
            {
                //calculate X
                float screenX = (matrix.m11 * pos.X) + (matrix.m12 * pos.Y) + (matrix.m13 * pos.Z) + matrix.m14;
                //calculate Y
                float screenY = (matrix.m21 * pos.X) +(matrix.m22 * pos.Y) + (matrix.m23 * pos.Z) + matrix.m24;
                //calculate camera center
                float camX = width / 2;
                float camY = height / 2;
                //perform perspective division and transformation
                float X = camX + (camX * screenX / screenW);
                float Y = camY + (camY * screenY / screenW);
                //return x and y
                screenCoordinates.X = X;
                screenCoordinates.Y = Y;
                return screenCoordinates;
            }
            else //return out of bounce vecotr if not in front
            {
                return new Vector2(-99, -99);
            }

        }

        void DrawMenu()
        {
            ImGui.Begin("Ludak's CS2 Cheat");

            if (ImGui.BeginTabBar("Tabs"))
            {
                //first page
                if (ImGui.BeginTabItem("General"))
                {
                    ImGui.Checkbox("Esp", ref enableEsp);
                    ImGui.EndTabItem();
                }

                //second page

                if (ImGui.BeginTabItem("Color"))
                {
                    ImGui.ColorPicker4("Team Color", ref teamColor);
                    ImGui.Checkbox("Team Line", ref enableTeamLine);
                    ImGui.Checkbox("Enemy Box", ref enableEnemyBox);
                    ImGui.Checkbox("Enemy Dot", ref enableEnemyDot);
                    ImGui.Checkbox("Enemy Healthbar", ref enableEnemyHealthBar);
                    ImGui.EndTabItem();

                }
            }
            ImGui.EndTabBar();
        }

        void DrawOverlay()
        {
            ImGui.SetNextWindowSize(WindowSize);
            ImGui.SetNextWindowPos(windowLocation);
            ImGui.Begin("overlay", ImGuiWindowFlags.NoDecoration
                    | ImGuiWindowFlags.NoBackground
                    | ImGuiWindowFlags.NoBringToFrontOnFocus
                    | ImGuiWindowFlags.NoMove
                    | ImGuiWindowFlags.NoInputs
                    | ImGuiWindowFlags.NoCollapse
                    | ImGuiWindowFlags.NoScrollbar
                    | ImGuiWindowFlags.NoScrollWithMouse
                    );
        }

        void MainLogic()
        {
            client = swed.GetModuleBase("client.dll");

            var window = GetWindowRect(swed.GetProcess().MainWindowHandle);
            windowLocation = new Vector2(window.left, window.top);
            WindowSize = Vector2.Subtract(new Vector2(window.right, window.bottom), windowLocation);
            lineOrigin = new Vector2(windowLocation.X + WindowSize.X / 2, window.bottom);
            windowCenter = new Vector2(lineOrigin.X, window.bottom - WindowSize.Y / 2);


            while (true)
            {
                ReloadEntities();

                Thread.Sleep(3);

             
            }
        }

        void ReloadEntities()
        {
            entities.Clear(); //clearllist
            playerTeam.Clear();
            enemyTeam.Clear();

            localPlayer.address = swed.ReadPointer(client, offsets.localPlayer); //set adress so we can update
            UpdateEntity(localPlayer); // update

            UpdateEntities();
        }

        void UpdateEntities() //handle other entities
        {
            for (int i = 0; i < 64; i++)
            {
                IntPtr tempEntityAdress = swed.ReadPointer(client, offsets.entityList + i * 0x08);

                if (tempEntityAdress == IntPtr.Zero)
                    continue; // skip if leading to adress

                Entity entity = new Entity();
                entity.address = tempEntityAdress;

                UpdateEntity(entity);

                if (entity.health < 1 || entity.health > 100)
                    continue;

                if (!entities.Any(element => element.origin.X == entity.origin.X))
                {
                    entities.Add(entity);

                    if (entity.teamNum == localPlayer.teamNum)
                    {
                        playerTeam.Add(entity);
                    }

                    else
                    {
                        enemyTeam.Add(entity);
                    }
                }
            }
        }

        void UpdateEntity(Entity entity)
            {
            //1d
            entity.health = swed.ReadInt(entity.address, offsets.health);
            entity.origin = swed.ReadVec(entity.address, offsets.origin);
            entity.teamNum = swed.ReadInt(entity.address, offsets.teamNum);

            //3d
            entity.origin = swed.ReadVec(entity.address, offsets.origin);
            Console.WriteLine(entity.health);
            Console.WriteLine(entity.origin);
            entity.viewOffset = new Vector3(0, 0, 65); //simulate view offset
            entity.abs = Vector3.Add(entity.origin, entity.viewOffset);
 


            var currentViewmatrix = ReadMatrix(client + offsets.viewMatrix);
            entity.originScreenPosition = Vector2.Add(WorldToScreen(currentViewmatrix, entity.origin, (int)WindowSize.X, (int)WindowSize.Y), windowLocation);
            entity.absScreenPosition = Vector2.Add(WorldToScreen(currentViewmatrix, entity.abs, (int)WindowSize.X, (int)WindowSize.Y), windowLocation);

        }
        static void Main(string[] args)
            {
                Program program = new Program();
                program.Start().Wait();

                Thread mainlogicThread = new Thread(program.MainLogic) { IsBackground = true };
                mainlogicThread.Start();
            }
        }
    }
        