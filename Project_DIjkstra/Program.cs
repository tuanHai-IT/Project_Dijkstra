using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace LANNetworkSimulation
{
    public enum DeviceType
    {
        Computer,
        Switch,
        Router
    }

    public class Device
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DeviceType Type { get; set; }
        public Point Position { get; set; }
        public Size Size { get; set; } = new Size(40, 40);

        public Rectangle GetBounds()
        {
            return new Rectangle(Position, Size);
        }

        public void Draw(Graphics g)
        {
            var bounds = GetBounds();
            Color deviceColor;

            switch (Type)
            {
                case DeviceType.Computer:
                    deviceColor = Color.LightBlue;
                    break;
                case DeviceType.Switch:
                    deviceColor = Color.LightGreen;
                    break;
                case DeviceType.Router:
                    deviceColor = Color.LightPink;
                    break;
                default:
                    deviceColor = Color.Gray;
                    break;
            }

            g.FillEllipse(new SolidBrush(deviceColor), bounds);
            g.DrawEllipse(Pens.Black, bounds);

            // Vẽ tên thiết bị
            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Center;
            g.DrawString(Name, new Font("Arial", 8), Brushes.Black, new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height), sf);
        }
    }

    public class Connection
    {
        public int Id { get; set; }
        public Device Source { get; set; }
        public Device Destination { get; set; }
        public double Bandwidth { get; set; } // Mbps
        public double Latency { get; set; }   // ms
        public bool IsHighlighted { get; set; } = false;

        public double CalculateTransferTime(double fileSize)
        {
            return Latency + (fileSize * 8 / Bandwidth * 1000); // Kết quả tính bằng ms
        }

        public void Draw(Graphics g)
        {
            Pen pen = IsHighlighted ? new Pen(Color.Red, 2) : new Pen(Color.Black);

            // Tính toán vị trí trung tâm của các thiết bị
            Point sourceCenter = new Point(
                Source.Position.X + Source.Size.Width / 2,
                Source.Position.Y + Source.Size.Height / 2);

            Point destCenter = new Point(
                Destination.Position.X + Destination.Size.Width / 2,
                Destination.Position.Y + Destination.Size.Height / 2);

            // Vẽ đường kết nối
            g.DrawLine(pen, sourceCenter, destCenter);

            // Hiển thị thông tin băng thông và độ trễ
            Point midPoint = new Point(
                (sourceCenter.X + destCenter.X) / 2,
                (sourceCenter.Y + destCenter.Y) / 2);

            string info = $"{Bandwidth} Mbps, {Latency} ms";
            g.DrawString(info, new Font("Arial", 7), Brushes.Blue, midPoint);
        }
    }

    public class Network
    {
        public List<Device> Devices { get; set; } = new List<Device>();
        public List<Connection> Connections { get; set; } = new List<Connection>();
        private int nextDeviceId = 1;
        private int nextConnectionId = 1;

        public Device AddDevice(string name, DeviceType type, Point position)
        {
            var device = new Device
            {
                Id = nextDeviceId++,
                Name = name,
                Type = type,
                Position = position
            };

            Devices.Add(device);
            return device;
        }

        public void RemoveDevice(Device device)
        {
            // Xóa các kết nối liên quan
            var relatedConnections = Connections
                .Where(c => c.Source == device || c.Destination == device)
                .ToList();

            foreach (var conn in relatedConnections)
            {
                Connections.Remove(conn);
            }

            Devices.Remove(device);
        }

        public Connection AddConnection(Device source, Device destination, double bandwidth, double latency)
        {
            // Kiểm tra kết nối đã tồn tại chưa
            if (Connections.Any(c =>
                (c.Source == source && c.Destination == destination) ||
                (c.Source == destination && c.Destination == source)))
            {
                return null;
            }

            var connection = new Connection
            {
                Id = nextConnectionId++,
                Source = source,
                Destination = destination,
                Bandwidth = bandwidth,
                Latency = latency
            };

            Connections.Add(connection);
            return connection;
        }

        public void RemoveConnection(Connection connection)
        {
            Connections.Remove(connection);
        }

        public void Draw(Graphics g)
        {
            // Vẽ các kết nối trước
            foreach (var connection in Connections)
            {
                connection.Draw(g);
            }

            // Sau đó vẽ các thiết bị
            foreach (var device in Devices)
            {
                device.Draw(g);
            }
        }

        public Device GetDeviceAt(Point point)
        {
            return Devices.FirstOrDefault(d => d.GetBounds().Contains(point));
        }

        // Thuật toán Dijkstra để tìm đường đi ngắn nhất
        public List<Device> FindShortestPath(Device source, Device destination)
        {
            // Dictionary để lưu khoảng cách từ nguồn đến mỗi thiết bị
            Dictionary<Device, double> distances = new Dictionary<Device, double>();

            // Dictionary để lưu thiết bị trước đó trong đường đi ngắn nhất
            Dictionary<Device, Device> previousDevices = new Dictionary<Device, Device>();

            // Danh sách các thiết bị chưa xét
            List<Device> unvisited = new List<Device>();

            // Khởi tạo
            foreach (var device in Devices)
            {
                distances[device] = double.MaxValue;
                previousDevices[device] = null;
                unvisited.Add(device);
            }

            distances[source] = 0;

            while (unvisited.Count > 0)
            {
                // Tìm thiết bị có khoảng cách nhỏ nhất
                Device current = null;
                double minDistance = double.MaxValue;

                foreach (var device in unvisited)
                {
                    if (distances[device] < minDistance)
                    {
                        minDistance = distances[device];
                        current = device;
                    }
                }

                // Nếu không tìm thấy thiết bị nào mới (không còn đường đi) hoặc tìm thấy đích
                if (current == null || current == destination || distances[current] == double.MaxValue)
                    break;

                unvisited.Remove(current);

                // Duyệt qua các thiết bị kề
                foreach (var connection in Connections.Where(c => c.Source == current || c.Destination == current))
                {
                    Device neighbor = connection.Source == current ? connection.Destination : connection.Source;

                    if (!unvisited.Contains(neighbor))
                        continue;

                    // Tính khoảng cách mới, sử dụng độ trễ làm trọng số
                    double newDistance = distances[current] + connection.Latency;

                    if (newDistance < distances[neighbor])
                    {
                        distances[neighbor] = newDistance;
                        previousDevices[neighbor] = current;
                    }
                }
            }

            // Tạo đường đi từ nguồn đến đích
            List<Device> path = new List<Device>();
            Device currentDevice = destination;

            // Nếu không tìm thấy đường đi
            if (previousDevices[destination] == null && destination != source)
                return path;

            while (currentDevice != null)
            {
                path.Insert(0, currentDevice);
                currentDevice = previousDevices[currentDevice];
            }

            return path;
        }

        // Tính toán thời gian truyền file tổng cộng
        public double CalculateTotalTransferTime(List<Device> path, double fileSize)
        {
            double totalTime = 0;

            // Reset highlighting
            foreach (var connection in Connections)
            {
                connection.IsHighlighted = false;
            }

            for (int i = 0; i < path.Count - 1; i++)
            {
                var connection = Connections.FirstOrDefault(c =>
                    (c.Source == path[i] && c.Destination == path[i + 1]) ||
                    (c.Source == path[i + 1] && c.Destination == path[i]));

                if (connection != null)
                {
                    totalTime += connection.CalculateTransferTime(fileSize);
                    connection.IsHighlighted = true;
                }
            }

            return totalTime;
        }

        public Connection GetConnectionAt(Point point)
        {
            const int clickTolerance = 5; // Độ chính xác của click chuột

            foreach (var connection in Connections)
            {
                // Tính toán vị trí trung tâm của các thiết bị
                Point sourceCenter = new Point(
                    connection.Source.Position.X + connection.Source.Size.Width / 2,
                    connection.Source.Position.Y + connection.Source.Size.Height / 2);

                Point destCenter = new Point(
                    connection.Destination.Position.X + connection.Destination.Size.Width / 2,
                    connection.Destination.Position.Y + connection.Destination.Size.Height / 2);

                // Kiểm tra khoảng cách từ điểm click đến đoạn thẳng
                if (DistanceToLine(point, sourceCenter, destCenter) <= clickTolerance)
                {
                    return connection;
                }
            }

            return null;
        }

        // Hàm tính khoảng cách từ điểm đến đoạn thẳng
        private double DistanceToLine(Point point, Point lineStart, Point lineEnd)
        {
            double lineLength = Math.Sqrt(Math.Pow(lineEnd.X - lineStart.X, 2) + Math.Pow(lineEnd.Y - lineStart.Y, 2));
            if (lineLength == 0) return double.MaxValue;

            double t = ((point.X - lineStart.X) * (lineEnd.X - lineStart.X) + (point.Y - lineStart.Y) * (lineEnd.Y - lineStart.Y)) / (lineLength * lineLength);

            if (t < 0) return Math.Sqrt(Math.Pow(point.X - lineStart.X, 2) + Math.Pow(point.Y - lineStart.Y, 2));
            if (t > 1) return Math.Sqrt(Math.Pow(point.X - lineEnd.X, 2) + Math.Pow(point.Y - lineEnd.Y, 2));

            double projectionX = lineStart.X + t * (lineEnd.X - lineStart.X);
            double projectionY = lineStart.Y + t * (lineEnd.Y - lineStart.Y);

            return Math.Sqrt(Math.Pow(point.X - projectionX, 2) + Math.Pow(point.Y - projectionY, 2));
        }
    }

    public partial class MainForm : Form
    {
        private Network network = new Network();
        private Device selectedDevice = null;
        private Connection selectedConnection = null;
        private DeviceType selectedDeviceType = DeviceType.Computer;
        private bool isAddingDevice = false;
        private bool isAddingConnection = false;
        private Device connectionSource = null;
        private bool isDraggingDevice = false;
        private Point dragOffset;

        // Thêm các control cho việc cập nhật
        private TextBox txtDeviceName;
        private ComboBox cboDeviceType;
        private TextBox txtBandwidth;
        private TextBox txtLatency;
        private Button btnUpdateDevice;
        private Button btnUpdateConnection;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Mô phỏng mạng LAN - Thuật toán Dijkstra |";
            this.Text += " Nhóm Cái Tôi Cao";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(255, 192, 192);

            // Khởi tạo các control
            Panel networkPanel = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(850, 600),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
            };
            this.Controls.Add(networkPanel);

            Panel controlPanel = new Panel
            {
                Location = new Point(870, 10),
                Size = new Size(300, 600),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(controlPanel);

            // Panel hiển thị kết quả
            Panel resultPanel = new Panel
            {
                Location = new Point(10, 620),
                Size = new Size(1160, 130),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
            };
            this.Controls.Add(resultPanel);

            // Các control trong Panel điều khiển
            int y = 10;

            Button btnReset = new Button
            {
                Text = "Reset Mạng",
                Location = new Point(10, 560), // Di chuyển xuống dưới trong panel điều khiển
                Size = new Size(280, 30),
                BackColor = Color.Red,
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            controlPanel.Controls.Add(btnReset);

            // GroupBox thiết bị
            GroupBox deviceGroup = new GroupBox
            {
                Text = "Quản lý thiết bị",
                Location = new Point(10, y),
                Size = new Size(280, 160), // Tăng kích thước để chứa nút cập nhật
                BackColor = Color.White
            };
            controlPanel.Controls.Add(deviceGroup);

            Label lblDeviceName = new Label
            {
                Text = "Tên thiết bị:",
                Location = new Point(10, 20),
                Size = new Size(80, 20)
            };
            deviceGroup.Controls.Add(lblDeviceName);

            txtDeviceName = new TextBox
            {
                Location = new Point(90, 20),
                Size = new Size(180, 20)
            };
            deviceGroup.Controls.Add(txtDeviceName);

            Label lblDeviceType = new Label
            {
                Text = "Loại thiết bị:",
                Location = new Point(10, 50),
                Size = new Size(80, 20)
            };
            deviceGroup.Controls.Add(lblDeviceType);

            cboDeviceType = new ComboBox
            {
                Location = new Point(90, 50),
                Size = new Size(180, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboDeviceType.Items.AddRange(Enum.GetNames(typeof(DeviceType)));
            cboDeviceType.SelectedIndex = 0;
            deviceGroup.Controls.Add(cboDeviceType);

            Button btnAddDevice = new Button
            {
                Text = "Thêm thiết bị",
                Location = new Point(10, 80),
                Size = new Size(120, 30)
            };
            deviceGroup.Controls.Add(btnAddDevice);

            Button btnRemoveDevice = new Button
            {
                Text = "Xóa thiết bị",
                Location = new Point(150, 80),
                Size = new Size(120, 30),
                Enabled = false
            };
            deviceGroup.Controls.Add(btnRemoveDevice);

            // Thêm nút cập nhật thiết bị
            btnUpdateDevice = new Button
            {
                Text = "Cập nhật thiết bị",
                Location = new Point(10, 120),
                Size = new Size(260, 30),
                Enabled = false
            };
            deviceGroup.Controls.Add(btnUpdateDevice);

            y += 170;

            // GroupBox kết nối
            GroupBox connectionGroup = new GroupBox
            {
                Text = "Quản lý kết nối",
                Location = new Point(10, y),
                Size = new Size(280, 160), // Tăng kích thước để chứa nút cập nhật
                BackColor = Color.White
            };
            controlPanel.Controls.Add(connectionGroup);

            Label lblBandwidth = new Label
            {
                Text = "Băng thông (Mbps):",
                Location = new Point(10, 20),
                Size = new Size(120, 20)
            };
            connectionGroup.Controls.Add(lblBandwidth);

            txtBandwidth = new TextBox
            {
                Location = new Point(130, 20),
                Size = new Size(140, 20),
                Text = "100"
            };
            connectionGroup.Controls.Add(txtBandwidth);

            Label lblLatency = new Label
            {
                Text = "Độ trễ (ms):",
                Location = new Point(10, 50),
                Size = new Size(120, 20)
            };
            connectionGroup.Controls.Add(lblLatency);

            txtLatency = new TextBox
            {
                Location = new Point(130, 50),
                Size = new Size(140, 20),
                Text = "5"
            };
            connectionGroup.Controls.Add(txtLatency);

            Button btnAddConnection = new Button
            {
                Text = "Thêm kết nối",
                Location = new Point(10, 80),
                Size = new Size(120, 30)
            };
            connectionGroup.Controls.Add(btnAddConnection);

            Button btnRemoveConnection = new Button
            {
                Text = "Xóa kết nối",
                Location = new Point(150, 80),
                Size = new Size(120, 30),
                Enabled = false
            };
            connectionGroup.Controls.Add(btnRemoveConnection);

            // Thêm nút cập nhật kết nối
            btnUpdateConnection = new Button
            {
                Text = "Cập nhật kết nối",
                Location = new Point(10, 120),
                Size = new Size(260, 30),
                Enabled = false
            };
            connectionGroup.Controls.Add(btnUpdateConnection);

            y += 170;

            // GroupBox tìm đường đi
            GroupBox pathFindingGroup = new GroupBox
            {
                Text = "Tìm đường đi",
                Location = new Point(10, y),
                Size = new Size(280, 200),
                BackColor = Color.White,
            };
            controlPanel.Controls.Add(pathFindingGroup);

            Label lblSource = new Label
            {
                Text = "Thiết bị nguồn:",
                Location = new Point(10, 20),
                Size = new Size(100, 20)
            };
            pathFindingGroup.Controls.Add(lblSource);

            ComboBox cboSource = new ComboBox
            {
                Location = new Point(110, 20),
                Size = new Size(160, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            pathFindingGroup.Controls.Add(cboSource);

            Label lblDestination = new Label
            {
                Text = "Thiết bị đích:",
                Location = new Point(10, 50),
                Size = new Size(100, 20)
            };
            pathFindingGroup.Controls.Add(lblDestination);

            ComboBox cboDestination = new ComboBox
            {
                Location = new Point(110, 50),
                Size = new Size(160, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            pathFindingGroup.Controls.Add(cboDestination);

            Label lblFileSize = new Label
            {
                Text = "Kích thước file (MB):",
                Location = new Point(10, 80),
                Size = new Size(120, 20)
            };
            pathFindingGroup.Controls.Add(lblFileSize);

            TextBox txtFileSize = new TextBox
            {
                Location = new Point(130, 80),
                Size = new Size(140, 20),
                Text = "100"
            };
            pathFindingGroup.Controls.Add(txtFileSize);

            Button btnFindPath = new Button
            {
                Text = "Tìm đường đi tối ưu",
                Location = new Point(10, 110),
                Size = new Size(260, 30)
            };
            pathFindingGroup.Controls.Add(btnFindPath);

            Button btnMultiTarget = new Button
            {
                Text = "Gửi đến nhiều máy",
                Location = new Point(10, 150),
                Size = new Size(260, 30)
            };
            pathFindingGroup.Controls.Add(btnMultiTarget);

            // Các control trong Panel kết quả
            Label lblResultTitle = new Label
            {
                Text = "Kết quả tìm đường:",
                Location = new Point(10, 10),
                Size = new Size(150, 20),
                Font = new Font("Arial", 12, FontStyle.Bold)
            };
            resultPanel.Controls.Add(lblResultTitle);

            TextBox txtResult = new TextBox
            {
                Location = new Point(10, 35),
                Size = new Size(1140, 85),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Arial", 14, FontStyle.Regular)
            };
            resultPanel.Controls.Add(txtResult);

            // Xử lý sự kiện
            networkPanel.Paint += (sender, e) =>
            {
                network.Draw(e.Graphics);
            };

            networkPanel.MouseDown += (sender, e) =>
            {
                if (isAddingDevice)
                {
                    // Thêm thiết bị mới
                    string name = txtDeviceName.Text.Trim();
                    if (string.IsNullOrEmpty(name))
                    {
                        MessageBox.Show("Vui lòng nhập tên thiết bị", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var device = network.AddDevice(name, selectedDeviceType, e.Location);
                    isAddingDevice = false;
                    RefreshDeviceLists(cboSource, cboDestination);
                    networkPanel.Invalidate();
                }
                else if (isAddingConnection)
                {
                    // Chọn thiết bị nguồn hoặc đích cho kết nối
                    var device = network.GetDeviceAt(e.Location);
                    if (device != null)
                    {
                        if (connectionSource == null)
                        {
                            connectionSource = device;
                            txtResult.Text = $"Đã chọn thiết bị nguồn: {device.Name}. Vui lòng chọn thiết bị đích.";
                        }
                        else if (device != connectionSource)
                        {
                            // Tạo kết nối mới
                            double bandwidth, latency;
                            if (!double.TryParse(txtBandwidth.Text, out bandwidth) || bandwidth <= 0)
                            {
                                MessageBox.Show("Vui lòng nhập băng thông hợp lệ", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                            if (!double.TryParse(txtLatency.Text, out latency) || latency <= 0)
                            {
                                MessageBox.Show("Vui lòng nhập độ trễ hợp lệ", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }

                            var connection = network.AddConnection(connectionSource, device, bandwidth, latency);
                            if (connection == null)
                            {
                                txtResult.Text = "Kết nối này đã tồn tại!";
                            }
                            else
                            {
                                txtResult.Text = $"Đã tạo kết nối từ {connectionSource.Name} đến {device.Name} với băng thông {bandwidth} Mbps và độ trễ {latency} ms";
                            }

                            isAddingConnection = false;
                            connectionSource = null;
                            networkPanel.Invalidate();
                        }
                    }
                }
                else
                {
                    // Chọn thiết bị để di chuyển, xóa hoặc cập nhật
                    var clickedDevice = network.GetDeviceAt(e.Location);
                    if (clickedDevice != null)
                    {
                        selectedDevice = clickedDevice;
                        btnRemoveDevice.Enabled = true;
                        btnUpdateDevice.Enabled = true;
                        isDraggingDevice = true;
                        dragOffset = new Point(e.X - selectedDevice.Position.X, e.Y - selectedDevice.Position.Y);

                        // Hiển thị thông tin thiết bị trong các control
                        txtDeviceName.Text = selectedDevice.Name;
                        cboDeviceType.SelectedItem = selectedDevice.Type.ToString();

                        // Cập nhật thông báo
                        txtResult.Text = $"Đã chọn thiết bị: {selectedDevice.Name} (Loại: {selectedDevice.Type})";

                        // Bỏ chọn kết nối nếu đang chọn thiết bị
                        selectedConnection = null;
                        btnRemoveConnection.Enabled = false;
                        btnUpdateConnection.Enabled = false;
                    }
                    else
                    {
                        // Kiểm tra xem có click vào kết nối không
                        var clickedConnection = network.GetConnectionAt(e.Location);
                        if (clickedConnection != null)
                        {
                            selectedConnection = clickedConnection;
                            btnRemoveConnection.Enabled = true;
                            btnUpdateConnection.Enabled = true;

                            // Hiển thị thông tin kết nối trong các control
                            txtBandwidth.Text = selectedConnection.Bandwidth.ToString();
                            txtLatency.Text = selectedConnection.Latency.ToString();

                            // Cập nhật thông báo
                            txtResult.Text = $"Đã chọn kết nối: {selectedConnection.Source.Name} ↔ {selectedConnection.Destination.Name}";
                            txtResult.Text += $"\nBăng thông: {selectedConnection.Bandwidth} Mbps, Độ trễ: {selectedConnection.Latency} ms";

                            // Bỏ chọn thiết bị nếu đang chọn kết nối
                            selectedDevice = null;
                            btnRemoveDevice.Enabled = false;
                            btnUpdateDevice.Enabled = false;
                        }
                        else
                        {
                            // Không chọn thiết bị và kết nối
                            selectedDevice = null;
                            selectedConnection = null;
                            btnRemoveDevice.Enabled = false;
                            btnUpdateDevice.Enabled = false;
                            btnRemoveConnection.Enabled = false;
                            btnUpdateConnection.Enabled = false;
                        }
                    }
                }
            };

            networkPanel.MouseMove += (sender, e) =>
            {
                if (isDraggingDevice && selectedDevice != null)
                {
                    selectedDevice.Position = new Point(e.X - dragOffset.X, e.Y - dragOffset.Y);
                    networkPanel.Invalidate();
                }
            };

            networkPanel.MouseUp += (sender, e) =>
            {
                isDraggingDevice = false;
            };

            btnAddDevice.Click += (sender, e) =>
            {
                isAddingDevice = true;
                isAddingConnection = false;
                connectionSource = null;
                selectedDeviceType = (DeviceType)Enum.Parse(typeof(DeviceType), cboDeviceType.SelectedItem.ToString());
                txtResult.Text = $"Vui lòng click vào panel mạng để thêm thiết bị {selectedDeviceType}";
            };

            btnRemoveDevice.Click += (sender, e) =>
            {
                if (selectedDevice != null)
                {
                    network.RemoveDevice(selectedDevice);
                    selectedDevice = null;
                    btnRemoveDevice.Enabled = false;
                    btnUpdateDevice.Enabled = false;
                    RefreshDeviceLists(cboSource, cboDestination);
                    networkPanel.Invalidate();
                    txtResult.Text = "Đã xóa thiết bị và các kết nối liên quan";
                }
            };

            btnAddConnection.Click += (sender, e) =>
            {
                isAddingConnection = true;
                isAddingDevice = false;
                connectionSource = null;
                txtResult.Text = "Vui lòng click vào thiết bị nguồn, sau đó click vào thiết bị đích";
            };

            btnRemoveConnection.Click += (sender, e) =>
            {
                if (selectedConnection != null)
                {
                    network.RemoveConnection(selectedConnection);
                    txtResult.Text = $"Đã xóa kết nối: {selectedConnection.Source.Name} ↔ {selectedConnection.Destination.Name}";
                    selectedConnection = null;
                    btnRemoveConnection.Enabled = false;
                    btnUpdateConnection.Enabled = false;
                    networkPanel.Invalidate();
                }
            };

            // Xử lý cập nhật thiết bị
            btnUpdateDevice.Click += (sender, e) =>
            {
                if (selectedDevice != null)
                {
                    string name = txtDeviceName.Text.Trim();
                    if (string.IsNullOrEmpty(name))
                    {
                        MessageBox.Show("Vui lòng nhập tên thiết bị", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    DeviceType newType = (DeviceType)Enum.Parse(typeof(DeviceType), cboDeviceType.SelectedItem.ToString());

                    // Cập nhật thông tin thiết bị
                    selectedDevice.Name = name;
                    selectedDevice.Type = newType;

                    // Cập nhật UI
                    RefreshDeviceLists(cboSource, cboDestination);
                    networkPanel.Invalidate();

                    txtResult.Text = $"Đã cập nhật thiết bị: {selectedDevice.Name} (Loại: {selectedDevice.Type})";
                }
            };

            // Xử lý cập nhật kết nối
            btnUpdateConnection.Click += (sender, e) =>
            {
                if (selectedConnection != null)
                {
                    double bandwidth, latency;
                    if (!double.TryParse(txtBandwidth.Text, out bandwidth) || bandwidth <= 0)
                    {
                        MessageBox.Show("Vui lòng nhập băng thông hợp lệ", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    if (!double.TryParse(txtLatency.Text, out latency) || latency <= 0)
                    {
                        MessageBox.Show("Vui lòng nhập độ trễ hợp lệ", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Cập nhật thông tin kết nối
                    selectedConnection.Bandwidth = bandwidth;
                    selectedConnection.Latency = latency;

                    // Cập nhật UI
                    networkPanel.Invalidate();

                    txtResult.Text = $"Đã cập nhật kết nối: {selectedConnection.Source.Name} ↔ {selectedConnection.Destination.Name}";
                    txtResult.Text += $"\nBăng thông mới: {bandwidth} Mbps, Độ trễ mới: {latency} ms";
                }
            };

            btnFindPath.Click += (sender, e) =>
            {
                if (cboSource.SelectedItem == null || cboDestination.SelectedItem == null)
                {
                    MessageBox.Show("Vui lòng chọn thiết bị nguồn và đích", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (cboSource.SelectedItem.ToString() == cboDestination.SelectedItem.ToString())
                {
                    txtResult.Text = "Trùng lặp thiết bị";
                    return;
                }

                double fileSize;
                if (!double.TryParse(txtFileSize.Text, out fileSize) || fileSize <= 0)
                {
                    MessageBox.Show("Vui lòng nhập kích thước file hợp lệ", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var source = network.Devices.FirstOrDefault(d => d.Name == cboSource.SelectedItem.ToString());
                var destination = network.Devices.FirstOrDefault(d => d.Name == cboDestination.SelectedItem.ToString());

                if (source != null && destination != null)
                {
                    var path = network.FindShortestPath(source, destination);
                    if (path.Count <= 1 && source != destination)
                    {
                        txtResult.Text = "Không tìm thấy đường đi từ " + source.Name + " đến " + destination.Name;
                    }
                    else
                    {
                        double totalTime = network.CalculateTotalTransferTime(path, fileSize);

                        string pathStr = string.Join(" → ", path.Select(d => d.Name));
                        txtResult.Text = $"Đường đi tối ưu: {pathStr}\r\n";
                        txtResult.Text += $"Tổng thời gian: {totalTime:F2} ms ({totalTime / 1000:F2} giây)\r\n";
                        txtResult.Text += $"Tổng số thiết bị trên đường đi: {path.Count}";

                        networkPanel.Invalidate();
                    }
                }
            };

            btnMultiTarget.Click += (sender, e) =>
            {
                if (cboSource.SelectedItem == null)
                {
                    MessageBox.Show("Vui lòng chọn thiết bị nguồn", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Chọn đến 3 máy đích
                var targets = new List<Device>();
                var computers = network.Devices
                    .Where(d => d.Type == DeviceType.Computer && d.Name != cboSource.SelectedItem.ToString())
                    .ToList();

                if (computers.Count < 3)
                {
                    MessageBox.Show("Cần có ít nhất 3 máy tính (không tính máy nguồn) trong mạng", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Lấy 3 máy tính đầu tiên làm mục tiêu
                targets = computers.Take(3).ToList();

                double fileSize;
                if (!double.TryParse(txtFileSize.Text, out fileSize) || fileSize <= 0)
                {
                    MessageBox.Show("Vui lòng nhập kích thước file hợp lệ", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var source = network.Devices.FirstOrDefault(d => d.Name == cboSource.SelectedItem.ToString());

                txtResult.Text = "Kết quả gửi file đến 3 máy sinh viên:\r\n";

                foreach (var connection in network.Connections)
                {
                    connection.IsHighlighted = false;
                }

                double totalTimeAll = 0;

                foreach (var target in targets)
                {
                    var path = network.FindShortestPath(source, target);
                    if (path.Count <= 1)
                    {
                        txtResult.Text += $"- Không tìm thấy đường đi từ {source.Name} đến {target.Name}\r\n";
                    }
                    else
                    {
                        double totalTime = 0;

                        for (int i = 0; i < path.Count - 1; i++)
                        {
                            var connection = network.Connections.FirstOrDefault(c =>
                                (c.Source == path[i] && c.Destination == path[i + 1]) ||
                                (c.Source == path[i + 1] && c.Destination == path[i]));

                            if (connection != null)
                            {
                                totalTime += connection.CalculateTransferTime(fileSize);
                                connection.IsHighlighted = true;
                            }
                        }

                        string pathStr = string.Join(" → ", path.Select(d => d.Name));
                        txtResult.Text += $"- Đến {target.Name}: {pathStr}, {totalTime:F2} ms ({totalTime / 1000:F2} giây)\r\n";

                        totalTimeAll = Math.Max(totalTimeAll, totalTime);
                    }
                }

                txtResult.Text += $"\r\nTổng thời gian để gửi file đến cả 3 máy: {totalTimeAll:F2} ms ({totalTimeAll / 1000:F2} giây)";
                networkPanel.Invalidate();
            };

            // Xử lý sự kiện khi nhấn nút Reset
            btnReset.Click += (sender, e) =>
            {
                // Hiển thị hộp thoại xác nhận
                DialogResult result = MessageBox.Show(
                    "Bạn có chắc chắn muốn xóa tất cả thiết bị và kết nối trong mạng?",
                    "Xác nhận reset",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    // Xóa tất cả thiết bị và kết nối
                    network.Devices.Clear();
                    network.Connections.Clear();

                    // Reset các controls và biến toàn cục
                    selectedDevice = null;
                    selectedConnection = null;
                    connectionSource = null;
                    isAddingDevice = false;
                    isAddingConnection = false;
                    isDraggingDevice = false;

                    // Cập nhật trạng thái các nút
                    btnRemoveDevice.Enabled = false;
                    btnUpdateDevice.Enabled = false;
                    btnRemoveConnection.Enabled = false;
                    btnUpdateConnection.Enabled = false;

                    // Cập nhật danh sách thiết bị trong ComboBox
                    RefreshDeviceLists(cboSource, cboDestination);

                    // Cập nhật thông báo
                    txtResult.Text = "Đã reset mạng. Tất cả thiết bị và kết nối đã được xóa.";

                    // Vẽ lại panel mạng
                    networkPanel.Invalidate();
                }
            };

            cboDeviceType.SelectedIndexChanged += (sender, e) =>
            {
                selectedDeviceType = (DeviceType)Enum.Parse(typeof(DeviceType), cboDeviceType.SelectedItem.ToString());
            };

            // Khởi tạo mạng mẫu
            CreateSampleNetwork(network);
            RefreshDeviceLists(cboSource, cboDestination);
            networkPanel.Invalidate();
        }

        private void RefreshDeviceLists(ComboBox cboSource, ComboBox cboDestination)
        {
            cboSource.Items.Clear();
            cboDestination.Items.Clear();

            foreach (var device in network.Devices)
            {
                cboSource.Items.Add(device.Name);
                cboDestination.Items.Add(device.Name);
            }

            if (cboSource.Items.Count > 0)
                cboSource.SelectedIndex = 0;

            if (cboDestination.Items.Count > 1)
                cboDestination.SelectedIndex = 1;
            else if (cboDestination.Items.Count > 0)
                cboDestination.SelectedIndex = 0;
        }

        private void CreateSampleNetwork(Network network)
        {
            // Tạo các thiết bị trên Panel
            var teacher = network.AddDevice("Giảng Viên", DeviceType.Computer, new Point(50, 50));
            var switch1 = network.AddDevice("Switch 1", DeviceType.Switch, new Point(200, 50));
            var switch2 = network.AddDevice("Switch 2", DeviceType.Switch, new Point(350, 150));
            var switch3 = network.AddDevice("Switch 3", DeviceType.Switch, new Point(500, 50));
            var router = network.AddDevice("Router", DeviceType.Router, new Point(350, 250));
            var student1 = network.AddDevice("Sinh viên 1", DeviceType.Computer, new Point(150, 300));
            var student2 = network.AddDevice("Sinh viên 2", DeviceType.Computer, new Point(350, 350));
            var student3 = network.AddDevice("Sinh viên 3", DeviceType.Computer, new Point(550, 300));
            //var student4 = network.AddDevice("Sinh viên 4", DeviceType.Computer, new Point(650, 150));

            // Tạo các kết nối
            network.AddConnection(teacher, switch1, 100, 2);
            network.AddConnection(switch1, switch2, 1000, 1);
            network.AddConnection(switch1, router, 100, 5);
            network.AddConnection(switch2, switch3, 1000, 1);
            network.AddConnection(switch2, router, 100, 3);
            //network.AddConnection(switch3, student4, 100, 2);
            network.AddConnection(switch3, router, 100, 4);
            network.AddConnection(router, student1, 100, 3);
            network.AddConnection(router, student2, 100, 2);
            network.AddConnection(router, student3, 100, 3);
        }
    }

    public class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}