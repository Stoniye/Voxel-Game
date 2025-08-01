using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Voxel_Game.res.scripts
{
    public class Chunk
    {
        public const int ChunkSize = 16;
        
        private readonly Dictionary<Vector2i, Chunk> _neighbors;
        
        private readonly byte[,,] _blocks;
        
        private int _vertexBufferObject;
        private int _vertexArrayObject;
        private int _elementBufferObject;
        private int _texture;
        
        private int _vertexCount;
        
        private readonly Vector3 _position;
        private readonly Shader _shader;
        
        private float[] _vertices;
        private uint[] _indices;

        public Chunk(Vector3 position, Shader shader, Dictionary<Vector2i, Chunk>? neighbors = null)
        {
            _position = position;
            _shader = shader;
            _blocks = new byte[ChunkSize, ChunkSize, ChunkSize];
            _neighbors = neighbors ?? new Dictionary<Vector2i, Chunk>();
            InitializeBlocks();
            LoadTexture();
        }

        private void InitializeBlocks()
        {
            for (int x = 0; x < ChunkSize; x++)
            {
                for (int y = 0; y < ChunkSize; y++)
                {
                    for (int z = 0; z < ChunkSize; z++)
                    {
                        if (y == 15)
                            _blocks[x, y, z] = 1; //Grass
                        else
                            _blocks[x, y, z] = 2; //Dirt
                    }
                }
            }
        }

        private void LoadTexture()
        {
            _texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _texture);

            float[] textureData = LoadTextureData("../../../res/textures/atlas/textureAtlas.raw");
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 256, 256, 0, PixelFormat.Rgba, PixelType.Float, textureData);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        }

        private float[] LoadTextureData(string path)
        {
            try
            {
                byte[] rawData = File.ReadAllBytes(path);
                int width = 256;
                int height = 256;
                float[] data = new float[width * height * 4];

                for (int i = 0; i < rawData.Length / 4; i++)
                {
                    data[i * 4 + 0] = rawData[i * 4 + 0] / 255.0f; // R
                    data[i * 4 + 1] = rawData[i * 4 + 1] / 255.0f; // G
                    data[i * 4 + 2] = rawData[i * 4 + 2] / 255.0f; // B
                    data[i * 4 + 3] = rawData[i * 4 + 3] / 255.0f; // A
                }
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load Texture Atlas: {ex.Message}");
                return new float[256 * 256 * 4]; //Fallback
            }
        }

        private void GenerateMesh()
        {
            List<float> vertices = new List<float>();
            List<uint> indices = new List<uint>();
            uint index = 0;

            for (int x = 0; x < ChunkSize; x++)
            {
                for (int y = 0; y < ChunkSize; y++)
                {
                    for (int z = 0; z < ChunkSize; z++)
                    {
                        if (_blocks[x, y, z] == 0) continue; //Skip if block is Air

                        Vector3 blockPos = new Vector3(x, y, z);
                        byte blockType = _blocks[x, y, z];
                        
                        Vector2[] texCoords = GetTextureCoords(blockType);

                        // Front face
                        if (BlockIsTransparent(x, y, z + 1))
                            AddFace(vertices, indices, blockPos, new Vector3(0, 0, 1), ref index, texCoords);

                        // Back face
                        if (BlockIsTransparent(x, y, z - 1))
                            AddFace(vertices, indices, blockPos, new Vector3(0, 0, -1), ref index, texCoords);

                        // Top face
                        if (BlockIsTransparent(x, y + 1, z))
                            AddFace(vertices, indices, blockPos, new Vector3(0, 1, 0), ref index, texCoords);

                        // Bottom face
                        if (BlockIsTransparent(x, y - 1, z))
                            AddFace(vertices, indices, blockPos, new Vector3(0, -1, 0), ref index, texCoords);

                        // Left face
                        if (BlockIsTransparent(x - 1, y, z))
                            AddFace(vertices, indices, blockPos, new Vector3(-1, 0, 0), ref index, texCoords);

                        // Right face
                        if (BlockIsTransparent(x + 1, y, z))
                            AddFace(vertices, indices, blockPos, new Vector3(1, 0, 0), ref index, texCoords);
                    }
                }
            }

            _vertices = vertices.ToArray();
            _indices = indices.ToArray();
            _vertexCount = indices.Count;
        }
        public void SetNeighbor(Vector2i relativePosition, Chunk neighbor)
        {
            _neighbors[relativePosition] = neighbor;
        }
        private Vector2[] GetTextureCoords(byte blockType)
        {
            int atlasSize = 16;
            float tileSize = 1.0f / atlasSize;
            
            int texIndex = blockType - 1;
            int texX = texIndex % atlasSize;
            int texY = texIndex / atlasSize;

            float u = texX * tileSize;
            float v = texY * tileSize;

            return new Vector2[]
            {
                new Vector2(u, v),
                new Vector2(u + tileSize, v),
                new Vector2(u + tileSize, v + tileSize),
                new Vector2(u, v + tileSize)
            };
        }

        private bool BlockIsTransparent(int x, int y, int z)
        {
            //Check if block in Chunk
            if ((x, y, z) is (>= 0 and < ChunkSize, >= 0 and < ChunkSize, >= 0 and < ChunkSize))
            {
                return _blocks[x, y, z] == 0;
            }
            
            //Block in other Chunk
            Vector2i dir = new Vector2i(
                x >= ChunkSize ? 1 : x < 0 ? -1 : 0,
                z >= ChunkSize ? 1 : z < 0 ? -1 : 0
            );
            Vector3 newBlockPos = new Vector3(
                (x % ChunkSize + ChunkSize) % ChunkSize,
                (y % ChunkSize + ChunkSize) % ChunkSize,
                (z % ChunkSize + ChunkSize) % ChunkSize
            );
                
            if (_neighbors.TryGetValue(dir, out Chunk? neighborChunk))
                return neighborChunk.GetBlock(newBlockPos) == 0;
            
            return true;
        }

        public void ReloadChunk()
        {
            GenerateMesh();
            SetupBuffers();
        }

        public byte GetBlock(Vector3 pos)
        {
            return _blocks[(int)pos.X, (int)pos.Y, (int)pos.Z];
        }

        private void AddFace(List<float> vertices, List<uint> indices, Vector3 pos, Vector3 normal, ref uint index, Vector2[] texCoords)
        {
            Vector3[] faceVertices = new Vector3[4];
            if (normal == new Vector3(0, 0, 1)) // Front
            {
                faceVertices[0] = pos + new Vector3(-0.5f, -0.5f, 0.5f);
                faceVertices[1] = pos + new Vector3(0.5f, -0.5f, 0.5f);
                faceVertices[2] = pos + new Vector3(0.5f, 0.5f, 0.5f);
                faceVertices[3] = pos + new Vector3(-0.5f, 0.5f, 0.5f);
            }
            else if (normal == new Vector3(0, 0, -1)) // Back
            {
                faceVertices[0] = pos + new Vector3(0.5f, -0.5f, -0.5f);
                faceVertices[1] = pos + new Vector3(-0.5f, -0.5f, -0.5f);
                faceVertices[2] = pos + new Vector3(-0.5f, 0.5f, -0.5f);
                faceVertices[3] = pos + new Vector3(0.5f, 0.5f, -0.5f);
            }
            else if (normal == new Vector3(0, 1, 0)) // Top
            {
                faceVertices[0] = pos + new Vector3(-0.5f, 0.5f, -0.5f);
                faceVertices[1] = pos + new Vector3(0.5f, 0.5f, -0.5f);
                faceVertices[2] = pos + new Vector3(0.5f, 0.5f, 0.5f);
                faceVertices[3] = pos + new Vector3(-0.5f, 0.5f, 0.5f);
            }
            else if (normal == new Vector3(0, -1, 0)) // Bottom
            {
                faceVertices[0] = pos + new Vector3(-0.5f, -0.5f, 0.5f);
                faceVertices[1] = pos + new Vector3(0.5f, -0.5f, 0.5f);
                faceVertices[2] = pos + new Vector3(0.5f, -0.5f, -0.5f);
                faceVertices[3] = pos + new Vector3(-0.5f, -0.5f, -0.5f);
            }
            else if (normal == new Vector3(-1, 0, 0)) // Left
            {
                faceVertices[0] = pos + new Vector3(-0.5f, -0.5f, -0.5f);
                faceVertices[1] = pos + new Vector3(-0.5f, -0.5f, 0.5f);
                faceVertices[2] = pos + new Vector3(-0.5f, 0.5f, 0.5f);
                faceVertices[3] = pos + new Vector3(-0.5f, 0.5f, -0.5f);
            }
            else if (normal == new Vector3(1, 0, 0)) // Right
            {
                faceVertices[0] = pos + new Vector3(0.5f, -0.5f, 0.5f);
                faceVertices[1] = pos + new Vector3(0.5f, -0.5f, -0.5f);
                faceVertices[2] = pos + new Vector3(0.5f, 0.5f, -0.5f);
                faceVertices[3] = pos + new Vector3(0.5f, 0.5f, 0.5f);
            }

            for (int i = 0; i < 4; i++)
            {
                vertices.Add(faceVertices[i].X);
                vertices.Add(faceVertices[i].Y);
                vertices.Add(faceVertices[i].Z);
                vertices.Add(1.0f); // Red
                vertices.Add(1.0f); // Green
                vertices.Add(1.0f); // Blue
                vertices.Add(texCoords[i].X);
                vertices.Add(texCoords[i].Y);
            }

            indices.Add(index + 0);
            indices.Add(index + 1);
            indices.Add(index + 2);
            indices.Add(index + 2);
            indices.Add(index + 3);
            indices.Add(index + 0);
            index += 4;
        }

        private void SetupBuffers()
        {
            _vertexArrayObject = GL.GenVertexArray();
            _vertexBufferObject = GL.GenBuffer();
            _elementBufferObject = GL.GenBuffer();

            GL.BindVertexArray(_vertexArrayObject);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);


            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);
        }

        public void Render(Matrix4 view, Matrix4 projection)
        {
            _shader.Use();
            Matrix4 model = Matrix4.CreateTranslation(_position);
            _shader.SetMatrix4("model", model);
            _shader.SetMatrix4("view", view);
            _shader.SetMatrix4("projection", projection);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            _shader.SetInt("textureAtlas", 0);

            GL.BindVertexArray(_vertexArrayObject);
            GL.DrawElements(PrimitiveType.Triangles, _vertexCount, DrawElementsType.UnsignedInt, 0);
        }

        public void Dispose()
        {
            GL.DeleteBuffer(_vertexBufferObject);
            GL.DeleteBuffer(_elementBufferObject);
            GL.DeleteVertexArray(_vertexArrayObject);
            GL.DeleteTexture(_texture);
        }
    }
}