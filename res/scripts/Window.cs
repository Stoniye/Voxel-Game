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
        private Vector3 _cameraPos = new Vector3(0.0f, 15.0f, 0.0f);
        private Vector3 _cameraFront = new Vector3(0.0f, 0.0f, -1.0f);
        readonly Vector3 _cameraUp = new Vector3(0.0f, 1.0f, 0.0f);
        private float _cameraSpeed = 10.0f;
        private readonly float _mouseSensitivity = 0.1f;
        private float _yaw = -90.0f;
        private float _pitch;
        private Vector2 _lastMousePos;
        
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

            _view = Matrix4.LookAt(_cameraPos, _cameraPos + _cameraFront, _cameraUp);
        
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
            float delta = (float)e.Time * _cameraSpeed;
            if (KeyboardState.IsKeyDown(Keys.W))
                _cameraPos += delta * _cameraFront;
            if (KeyboardState.IsKeyDown(Keys.S))
                _cameraPos -= delta * _cameraFront;
            if (KeyboardState.IsKeyDown(Keys.A))
                _cameraPos -= delta * Vector3.Normalize(Vector3.Cross(_cameraFront, _cameraUp));
            if (KeyboardState.IsKeyDown(Keys.D))
                _cameraPos += delta * Vector3.Normalize(Vector3.Cross(_cameraFront, _cameraUp));
            if (KeyboardState.IsKeyDown(Keys.Space))
                _cameraPos += delta * _cameraUp;
            if (KeyboardState.IsKeyDown(Keys.LeftControl))
                _cameraPos -= delta * _cameraUp;
            
            if (KeyboardState.IsKeyDown(Keys.LeftShift))
                _cameraSpeed = 20.0f;
            else
                _cameraSpeed = 10.0f;
            
            if (KeyboardState.IsKeyDown(Keys.Escape)) //Tab out
                CursorState = CursorState.Normal;
            
            //Close Window
            if (KeyboardState.IsKeyDown(Keys.Backspace))
            {
                CursorState = CursorState.Normal;
                Close();
            }
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