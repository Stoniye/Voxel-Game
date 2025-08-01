using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Voxel_Game.res.scripts
{
    public class Window : GameWindow
    {
        //Const
        private const float Fov = 90.0f;
        
        //Camera Variables
        private Vector3 _cameraFront = new Vector3(0.0f, 0.0f, -1.0f);
        readonly Vector3 _cameraUp = new Vector3(0.0f, 1.0f, 0.0f);
        private readonly float _mouseSensitivity = 0.1f;
        private float _yaw = -90.0f;
        private float _pitch;
        private Vector2 _lastMousePos;
        
        //Player Variables
        private Vector3 _playerPos = new Vector3(0.0f, 20.0f, 0.0f);
        private float _verticalVelocity = 0.0f;
        private bool _isGrounded = false;
        private const float Gravity = -9.81f;
        private const float JumpStrength = 5.0f;
        private const float PlayerHeight = 1.8f;
        private const float PlayerRadius = 0.3f;
        private const float PlayerSpeed = 2.0f;
        private const float PlayerSpeedSprint = 4.0f;
        
        //OpenGL
        private Shader _shader;
        private Matrix4 _view, _projection;
        
        //Other Variables
        private readonly Dictionary<Vector2i, Chunk> _chunks;

        public Window(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings() { ClientSize = (width, height), Title = title })
        {
            CenterWindow();
            CursorState = CursorState.Grabbed;
            _chunks = new Dictionary<Vector2i, Chunk>();
        }
        
        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.5f, 0.7f, 0.9f, 1.0f);
            GL.Enable(EnableCap.DepthTest);

            _shader = new Shader("../../../res/shaders/shader.vert", "../../../res/shaders/shader.frag");
            _shader.Use();
            
            GenerateWorld();

            _projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(Fov), Size.X / (float)Size.Y, 0.1f, 100.0f);
            _shader.SetMatrix4("projection", _projection);
        }

        private void GenerateWorld()
        {
            const int chunksX = 3;
            const int chunksZ = 3;
            
            //Generate Chunks
            for (int x = 0; x <= chunksX; x++)
            {
                for (int z = 0; z <= chunksZ; z++)
                {
                    Vector3 chunkPos = new Vector3(x * Chunk.ChunkSize, 0, z * Chunk.ChunkSize);
                    Chunk newChunk = new Chunk(chunkPos, _shader);
                    _chunks.Add(new Vector2i(x, z), newChunk);
                }
            }
            
            //Assign Chunk neighbors
            for (int x = 0; x <= chunksX; x++)
            {
                for (int z = 0; z <= chunksZ; z++)
                {
                    Chunk currentChunk = _chunks[new Vector2i(x, z)];
                    Vector2i[] neighborOffsets = new Vector2i[]
                    {
                        new Vector2i(0, 1),
                        new Vector2i(0, -1),
                        new Vector2i(-1, 0),
                        new Vector2i(1, 0)
                    };

                    foreach (Vector2i offset in neighborOffsets)
                    {
                        Vector2i neighborCoord = new Vector2i(x + offset.X, z + offset.Y);
                        
                        if (_chunks.TryGetValue(neighborCoord, out Chunk? neighborChunk))
                        {
                            if (neighborChunk != currentChunk)
                            {
                                currentChunk.SetNeighbor(offset, neighborChunk);
                            }
                        }
                    }
                    
                    currentChunk.ReloadChunk();
                }
            }
        }
        
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            _view = Matrix4.LookAt(_playerPos, _playerPos + _cameraFront, _cameraUp);
        
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            foreach (var chunk in _chunks.Values)
            {
                chunk.Render(_view, _projection);
            }

            SwapBuffers();
        }
        
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            
            if (CursorState == CursorState.Normal) //Tabbed out
            {
                if (MouseState.IsButtonDown(MouseButton.Left)) //Tab back in
                    CursorState = CursorState.Grabbed;

                return;
            }
            
            //Movement
            float delta = (float)e.Time;
            _isGrounded = IsGrounded();
            
            //Add Gravity force
            if (!_isGrounded)
                _verticalVelocity += Gravity * delta;
            else
                _verticalVelocity = 0.0f;
            
            //Jumping
            if (KeyboardState.IsKeyDown(Keys.Space) && _isGrounded)
            {
                _verticalVelocity = JumpStrength;
                _isGrounded = false;
            }
            
            //Apply vertical velocity
            Vector3 newCameraPos = _playerPos + new Vector3(0.0f, _verticalVelocity * delta, 0.0f);

            //Movement Input
            float cameraSpeed = KeyboardState.IsKeyDown(Keys.LeftShift) ? PlayerSpeedSprint : PlayerSpeed;
            Vector3 moveDir = Vector3.Zero;
            
            if (KeyboardState.IsKeyDown(Keys.W))
                moveDir += _cameraFront;
            if (KeyboardState.IsKeyDown(Keys.S))
                moveDir -= _cameraFront;
            if (KeyboardState.IsKeyDown(Keys.A))
                moveDir -= Vector3.Normalize(Vector3.Cross(_cameraFront, _cameraUp));
            if (KeyboardState.IsKeyDown(Keys.D))
                moveDir += Vector3.Normalize(Vector3.Cross(_cameraFront, _cameraUp));

            if (moveDir != Vector3.Zero)
                moveDir = Vector3.Normalize(moveDir) * cameraSpeed * delta;

            //Collision detection
            newCameraPos = MoveWithCollision(newCameraPos, moveDir); //Horizontal
            newCameraPos = ResolveVerticalCollision(newCameraPos); //Vertical

            _playerPos = newCameraPos;

            if (KeyboardState.IsKeyDown(Keys.Escape)) // Tab out
                CursorState = CursorState.Normal;
            
            //Close window
            if (KeyboardState.IsKeyDown(Keys.Backspace))
            {
                CursorState = CursorState.Normal;
                Close();
            }
        }
        
        private bool IsGrounded()
        {
            Vector3 playerFeetPos = _playerPos - new Vector3(0.0f, PlayerHeight / 2.0f + 0.1f, 0.0f); //+ 0.1f offset to avoid glitching into the ground
            return IsBlockAt(playerFeetPos);
        }

        private bool IsBlockAt(Vector3 worldPos)
        {
            Vector2i chunkCoord = new Vector2i(
                (int)Math.Floor(worldPos.X / Chunk.ChunkSize),
                (int)Math.Floor(worldPos.Z / Chunk.ChunkSize)
            );
            Vector3 localPos = new Vector3(
                (worldPos.X % Chunk.ChunkSize + Chunk.ChunkSize) % Chunk.ChunkSize,
                worldPos.Y,
                (worldPos.Z % Chunk.ChunkSize + Chunk.ChunkSize) % Chunk.ChunkSize
            );

            if (_chunks.TryGetValue(chunkCoord, out Chunk? chunk))
            {
                if (localPos.Y is >= 0 and < Chunk.ChunkSize)
                {
                    return chunk.GetBlock(localPos) != 0;
                }
            }
            return false;
        }
        
        private Vector3 MoveWithCollision(Vector3 newPos, Vector3 moveDir)
        {
            Vector3 resultPos = newPos;
            
            Vector3[] axes = {new Vector3(moveDir.X, 0, 0), new Vector3(0, moveDir.Y, 0), new Vector3(0, 0, moveDir.Z) };
            for (int i = 0; i < axes.Length; i++) //Check every movement direction
            {
                if (axes[i] == Vector3.Zero) continue;

                Vector3 testPos = resultPos + axes[i];
                
                Vector3 testMin = testPos - new Vector3(PlayerRadius, PlayerHeight / 2.0f, PlayerRadius);
                Vector3 testMax = testPos + new Vector3(PlayerRadius, PlayerHeight / 2.0f, PlayerRadius);

                if (!IsColliding(testMin, testMax))
                {
                    resultPos = testPos;
                }
            }

            return resultPos;
        }

        private Vector3 ResolveVerticalCollision(Vector3 newPos)
        {
            Vector3 aabbMin = newPos - new Vector3(PlayerRadius, PlayerHeight / 2.0f, PlayerRadius);
            Vector3 aabbMax = newPos + new Vector3(PlayerRadius, PlayerHeight / 2.0f, PlayerRadius);

            if (IsColliding(aabbMin, aabbMax)) //Test if Player Collides vertical
            {
                Vector3 upPos = newPos + new Vector3(0.0f, 0.1f, 0.0f);
                Vector3 downPos = newPos - new Vector3(0.0f, 0.1f, 0.0f);

                Vector3 upMin = upPos - new Vector3(PlayerRadius, PlayerHeight / 2.0f, PlayerRadius);
                Vector3 upMax = upPos + new Vector3(PlayerRadius, PlayerHeight / 2.0f, PlayerRadius);
                Vector3 downMin = downPos - new Vector3(PlayerRadius, PlayerHeight / 2.0f, PlayerRadius);
                Vector3 downMax = downPos + new Vector3(PlayerRadius, PlayerHeight / 2.0f, PlayerRadius);

                if (!IsColliding(upMin, upMax)) //Test if Player collides upwards
                {
                    _verticalVelocity = 0.0f;
                    return upPos;
                }
                
                if (!IsColliding(downMin, downMax)) //Test if Player collides downwards
                {
                    _verticalVelocity = 0.0f;
                    return downPos;
                }
                
                return _playerPos;
            }

            return newPos;
        }

        private bool IsColliding(Vector3 aabbMin, Vector3 aabbMax)
        {
            int minX = (int)Math.Floor(aabbMin.X);
            int maxX = (int)Math.Ceiling(aabbMax.X);
            int minY = (int)Math.Floor(aabbMin.Y);
            int maxY = (int)Math.Ceiling(aabbMax.Y);
            int minZ = (int)Math.Floor(aabbMin.Z);
            int maxZ = (int)Math.Ceiling(aabbMax.Z);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        if (IsBlockAt(new Vector3(x, y, z)))
                        {
                            return true; //Collision
                        }
                    }
                }
            }
            return false;
        }
        
        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);

            if (CursorState == CursorState.Normal) return; //Tabbed out

            float deltaX = e.X - _lastMousePos.X;
            float deltaY = _lastMousePos.Y - e.Y;
            _lastMousePos = new Vector2(e.X, e.Y);
            
            _yaw += deltaX * _mouseSensitivity;
            _pitch += deltaY * _mouseSensitivity;

            _pitch = MathHelper.Clamp(_pitch, -89.0f, 89.0f);

            _cameraFront = Vector3.Normalize(new Vector3(
                (float)(Math.Cos(MathHelper.DegreesToRadians(_yaw)) * Math.Cos(MathHelper.DegreesToRadians(_pitch))),
                (float)Math.Sin(MathHelper.DegreesToRadians(_pitch)),
                (float)(Math.Sin(MathHelper.DegreesToRadians(_yaw)) * Math.Cos(MathHelper.DegreesToRadians(_pitch)))
            ));
        }
        
        protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
        {
            base.OnFramebufferResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
            _projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(Fov), e.Width / (float)e.Height, 0.1f, 200.0f);
            _shader.SetMatrix4("projection", _projection);
        }

        protected override void OnUnload()
        {
            foreach (var chunk in _chunks.Values)
            {
                chunk.Dispose();
            }
            _shader.Dispose();
            base.OnUnload();
        }
    }
}