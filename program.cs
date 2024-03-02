using ClickableTransparentOverlay;
using HUNT_S_CHEAT_CS2;
using ImGuiNET;
using Swed64;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid.OpenGLBinding;



namespace CS2CHEAT
{

    
    class Program : Overlay
    {


        // imports and struct

        [DllImport("user32.dll")] // hotkey import

        static extern short GetAsyncKeyState(int vKey);


        [DllImport("user32.dll")]

        static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [StructLayout(LayoutKind.Sequential)]

        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;

        }

        public RECT GetWindowRect(IntPtr hWnd)
        {
            RECT rect = new RECT();
            GetWindowRect(hWnd, out rect);
            return rect;
        }


        // important class variables



        Swed swed = new Swed("cs2");
        Offsets offsets = new Offsets();
        ImDrawListPtr drawList;


        Entity localPlayer = new Entity();
        List<Entity> entities = new List<Entity>();
        List<Entity> enemyTeam = new List<Entity>();
        List<Entity> playerTeam = new List<Entity>();




        IntPtr client;

        // constants

        const int AIMBOT_HOTKEY = 0x06; // xbutton2, or mouse 5

        // other Vectors

        Vector3 offsetVector = new Vector3(0, 0, 10); // subtract 10 units from the height of the character



        // Global colors

        Vector4 teamColor = new Vector4(0, 0, 1, 1); // RGB, blue team mates
        Vector4 enemyColor = new Vector4(1, 0, 0, 1); // enemy red
        Vector4 healthBarColor = new Vector4(0, 1, 0, 1); // green
        Vector4 healthTextColor = new Vector4(0, 0, 0, 1); // black


        // screen variable, update later

        Vector2 windowLocation = new Vector2(0, 0);
        Vector2 WindowSize = new Vector2(1920, 1000);
        Vector2 lineOrigin = new Vector2(1920 / 2, 1000);
        Vector2 windowCenter = new Vector2(1920 / 2, 1000 / 2);

        // ImGui checkboxes and shit

        bool enableEsp = true;
        bool enableAimbot = true;

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

            Console.WriteLine("INJECTING UI CHEAT..");
            Thread.Sleep(3000);
            DrawMenu();
            DrawOverlay();
            Esp();
            Console.Clear();
            Console.WriteLine("SECESSFULLY INJECTED!");
            ImGui.End();


            // only render stuff here
        }



        void Aimbot() // main logic for aimbot
        {
           if (GetAsyncKeyState(AIMBOT_HOTKEY) < 0 && enableAimbot) // if hotkey is down and bool is enabled
            {
                if (enemyTeam.Count > 0)
                {
                    // aim at nearest enemy

                    var angles = CalculateAngles(localPlayer.origin, Vector3.Subtract(enemyTeam[0].origin, offsetVector));
                    AimAt(angles); // aim at the enemy
                }
            }
        }

        void AimAt(Vector3 angles)
        {
            swed.WriteFloat(client, offsets.viewAngles, angles.Y); // Y was before X this time
            swed.WriteFloat(client,offsets.viewAngles + 0x4, angles.X); // a float is 4 bytes, which means to get to the next value we skip 4 bytes.
        }


        Vector3 CalculateAngles(Vector3 from, Vector3 destintion)
        {
            float yaw;
            float pitch;


            // calculate yaw

            float deltaX = destintion.X - from.X;
            float deltaY = destintion.Y - from.Y;
            yaw = (float)(Math.Atan2(deltaY, deltaX) * 180 / Math.PI); // we use triangles



            // calculate the pitch

            float deltaZ = destintion.Z - from.Z;
            double distance = Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));
            pitch = -(float)(Math.Pow(deltaZ, distance) * 180 / Math.PI);



            // return angles


            return new Vector3(yaw, pitch, 0);


        }

        IntPtr entityList;


        float CalculateMagnitude(Vector3 v1, Vector3 v2)
        {
            return (float)Math.Sqrt(Math.Pow(v2.X - v1.X, 2) + Math.Pow(v2.Y - v1.Y, 2) + Math.Pow(v2.Z, 2));
        }



        void Esp()
        {
            // finally!!!!! :)

            drawList = ImGui.GetWindowDrawList(); // important to get the overlay

            if (enableEsp)
            {
                try // bad fix for stuff breaking but wtv.
                {
                    foreach (var entity in entities)
                    {
                        if (entity.teamNum == localPlayer.teamNum)
                        {
                            DrawVisuals(entity, teamColor, enableTeamLine, enableTeamBox, enableTeamDot, enableTeamHealthBar, enableTeamDistance);
                        }
                        else
                        {
                            DrawVisuals(entity, enemyColor, enableEnemyLine, enableEnemyBox, enableEnemyDot, enableEnemyHealthBar, enableEnemyDistance);
                        }
                    }
                } catch { }
            }
        }


        void DrawVisuals(Entity entity, Vector4 color, bool line, bool box, bool dot, bool healthBar, bool distance)
        {
            // check if 2d position vaild
            if (IsPixelInsideScreen(entity.originScreenPosition))
            {



                // convert our colors to uints

                uint uintColor = ImGui.ColorConvertFloat4ToU32(color);
                uint uintHealthTextColor = ImGui.ColorConvertFloat4ToU32(healthTextColor);
                uint uintHealthBarColor = ImGui.ColorConvertFloat4ToU32(healthBarColor);


                // calculate box attributes

                Vector2 boxWidth = new Vector2((entity.originScreenPosition.Y - entity.absScreenPosition.Y) / 2, 0f); // devide height By 2 to simulate width.
                Vector2 boxStart = Vector2.Subtract(entity.absScreenPosition, boxWidth); // get left topmost I think
                Vector2 boxEnd = Vector2.Add(entity.originScreenPosition, boxWidth); // get bottom right


                // calculate health bar stuff

                float barPercent = entity.health / 100f;
                Vector2 barHeight = new Vector2(0, barPercent * (entity.originScreenPosition.Y - entity.absScreenPosition.Y)); // calculate height like before, but with 2d coords
                Vector2 barStart = Vector2.Subtract(Vector2.Subtract(entity.originScreenPosition, boxWidth),barHeight); // get position beside box using the box width
                Vector2 barEnd = Vector2.Subtract(entity.originScreenPosition, Vector2.Add(boxWidth, new Vector2 (-4, 0))); // get the bottom right end of the bar

                // finally draw

                if (line)
                {
                    drawList.AddLine(lineOrigin, entity.originScreenPosition, uintColor, 3); // draw line to feet of entities
                }
                if (box)
                {
                    drawList.AddRect(boxStart, boxEnd, uintColor, 3); // box around character
                }
                if (dot)
                {
                    drawList.AddCircleFilled(entity.originScreenPosition, 5, uintColor);
                }

                if (healthBar)
                {
                    drawList.AddText(entity.originScreenPosition, uintHealthTextColor, $"hp: {entity.health}");
                    drawList.AddRectFilled(barStart, barEnd, uintHealthBarColor);

                }


            }

        }

        bool IsPixelInsideScreen(Vector2 pixel)
        {
            return pixel.X > windowLocation.X && pixel.X < windowLocation.X + WindowSize.X && pixel.Y > windowLocation.Y && pixel.Y < WindowSize.Y + windowLocation.Y; // check all windeow bounds
        }

        ViewMaxtrix ReadMatrix(IntPtr matrixAddress)
        {
            var viewMaxtrix = new ViewMaxtrix();
            var floatMaxtrix = swed.ReadMatrix(matrixAddress);



            // convert floats to our own viewmatrix type

            viewMaxtrix.m11 = floatMaxtrix[0];
            viewMaxtrix.m12 = floatMaxtrix[1];
            viewMaxtrix.m13 = floatMaxtrix[2];
            viewMaxtrix.m14 = floatMaxtrix[3];

            viewMaxtrix.m21 = floatMaxtrix[4];
            viewMaxtrix.m22 = floatMaxtrix[5];
            viewMaxtrix.m23 = floatMaxtrix[6];
            viewMaxtrix.m24 = floatMaxtrix[7];

            viewMaxtrix.m31 = floatMaxtrix[8];
            viewMaxtrix.m32 = floatMaxtrix[9];
            viewMaxtrix.m33 = floatMaxtrix[10];
            viewMaxtrix.m34 = floatMaxtrix[11];

            viewMaxtrix.m41 = floatMaxtrix[12];
            viewMaxtrix.m42 = floatMaxtrix[13];
            viewMaxtrix.m43 = floatMaxtrix[14];
            viewMaxtrix.m44 = floatMaxtrix[15];

            return viewMaxtrix;
        }

        Vector2 WorldToScreen(ViewMaxtrix matrix, Vector3 pos, int width, int height)
        {
            Vector2 screenCoorddinates = new Vector2();

            // calculate sacreen

            float screenW = (matrix.m41 * pos.X) + (matrix.m42 * pos.Y) + (matrix.m43 * pos.Z) + matrix.m44;

            if (screenW > 0.001f) // check that entity is in front of us 
            {
                // calculate X

                float screenX = (matrix.m11 * pos.X) + (matrix.m12 * pos.Y) + (matrix.m13 * pos.Z) + matrix.m14;

                // Calculate Y

                float screenY = (matrix.m21 * pos.X) + (matrix.m22 * pos.Y) + (matrix.m23 * pos.Z) + matrix.m24;

                // calculate camera center

                float camX = width / 2;
                float camY = height / 2;

                // perform perspective division and trasformation

                float X = camX + (camX * screenX / screenW);
                float Y = camY - (camY * screenY / screenW);



                screenCoorddinates.X = X;
                screenCoorddinates.Y = Y;
                return screenCoorddinates;
            }
            else // return out of bounce vector if not in front of us
            {
                return new Vector2 (-99, -99);
            }
        }


        void DrawMenu()
        {
            ImGui.Begin("HUNT CHEATS");

            if (ImGui.BeginTabBar("Tabs"))
            {

                // first page

                if (ImGui.BeginTabItem("General"))
                {
                    ImGui.Checkbox("Esp", ref enableEsp);
                    ImGui.Checkbox("Aimbot", ref enableAimbot);
                    ImGui.EndTabItem();
                }

                // second page

                if (ImGui.BeginTabItem("Visuals"))



                    // Team Colors
                ImGui.ColorPicker4("Team Color", ref teamColor);
                ImGui.Checkbox("Team Line", ref enableTeamLine);
                ImGui.Checkbox("Team Box", ref enableTeamBox);
                ImGui.Checkbox("Team Dot", ref enableTeamDot);
                ImGui.Checkbox("Team Healthbar", ref enableTeamHealthBar);


                // Enemy Colors
                ImGui.Checkbox("Enemy Line", ref enableEnemyLine);
                ImGui.Checkbox("Enemy Box", ref enableEnemyBox);
                ImGui.Checkbox("Enemy Dot", ref enableEnemyDot);
                ImGui.Checkbox("Enemy Healthbar", ref enableEnemyHealthBar);
                ImGui.EndTabItem();



             
            }
            ImGui.EndTabBar();
        }

        void DrawOverlay() // draw new window over game, (overlay)
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



            // calculate window position and size so we can place overlay on it
            var window = GetWindowRect(swed.GetProcess().MainWindowHandle);
            windowLocation = new Vector2(window.left,window.top);
            WindowSize = Vector2.Subtract(new Vector2(window.right,window.bottom),windowLocation);
            lineOrigin = new Vector2(windowLocation.X + WindowSize.X/2, window.bottom);
            windowCenter = new Vector2(lineOrigin.X, window.bottom - WindowSize.Y / 2);

            client = swed.GetModuleBase("client.dll");




            while (true) // always run
            {



                ReloadEnitites();

                if (enableAimbot)
                {
                    Aimbot();
                }

                Thread.Sleep(3);

 


            }
        }


        void ReloadEnitites()
        {
            entities.Clear(); // clear lists
            playerTeam.Clear();
            enemyTeam.Clear();

            localPlayer.address = swed.ReadPointer(client, offsets.localPlayer); // set the addess so we can update
            UpdateEntity(localPlayer); // update

            UpdateEntites();

            enemyTeam = enemyTeam.OrderBy(o => o.magnitude).ToList(); // sort entities
        }
        void UpdateEntites() // handle all other entites
        {
            for (int i = 0; i  < 64;i++) // normally less then 64 ents
            {
                IntPtr tempEntityAddress = swed.ReadPointer(client, offsets.entityList + i * 0x08);

                if (tempEntityAddress == IntPtr.Zero)
                    continue; // skip if 




                Entity entity = new Entity();
                entity.address = tempEntityAddress;

                UpdateEntity(entity);

                if (entity.health < 1 || entity.health > 100)
                            continue; // another check but now if entity is dead

                if (!entities.Any(element => element.origin.X == entity.origin.X)) // check if there is a duplicate of the entity since we use goofy entitylist
                {
                    entities.Add(entity);

                    if (entity.teamNum == localPlayer.teamNum) // also add to specific teams and enemies
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
            // 3d
            entity.origin = swed.ReadVec(entity.address, offsets.origin);
            entity.viewOffset = new Vector3(0, 0, 65);
            entity.abs = Vector3.Add(entity.origin, entity.viewOffset);


            // 2d

            var currentViewmatrix = ReadMatrix(client + offsets.viewMaxtrix);
            entity.originScreenPosition = Vector2.Add(WorldToScreen(currentViewmatrix, entity.origin, (int)WindowSize.X, (int)WindowSize.Y), windowLocation);
            entity.absScreenPosition = Vector2.Add(WorldToScreen(currentViewmatrix, entity.abs, (int)WindowSize.X, (int)WindowSize.Y), windowLocation);


            // 1d
            entity.health = swed.ReadInt(entity.address, offsets.health);
            entity.origin = swed.ReadVec(entity.address, offsets.origin);
            entity.teamNum = swed.ReadInt(entity.address, offsets.teamNum);
            entity.magnitude = CalculateMagnitude(localPlayer.origin, entity.origin);

        }

        
        static void Main(string[] args)
        {
            // run logic methods and more

            Program program = new Program(); 
            program.Start().Wait();
   
            Thread mainLogicThread = new Thread(program.MainLogic) { IsBackground = true }; // logic thread
            mainLogicThread.Start();
        }
    }
}
