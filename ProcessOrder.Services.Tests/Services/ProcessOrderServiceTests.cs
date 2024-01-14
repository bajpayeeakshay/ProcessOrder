using AutoFixture;
using Moq;
using NUnit.Framework;
using ProcessOrder.Services.Models;
using ProcessOrder.Services.Models.Settings;
using ProcessOrder.Services.Services;
using Serilog;

namespace ProcessOrder.Services.Tests.Services
{
    [TestFixture]
    public class ProcessOrderServiceTests
    {
        private MockRepository _mockRepository;
        private Fixture _fixture;
        private Mock<IProcessERP> _mockProcessERP;
        private Mock<ILogger> _mockLogger;
        private FileFormatSettings _fileFormatSettings;
        private AppSettings _appSettings;
        private Mock<IEmailNotifier> _mockEmailNotifier;

        [SetUp]
        public void SetUp()
        {
            _fixture = new Fixture();
            _mockRepository = new MockRepository(MockBehavior.Strict);
            _mockProcessERP = _mockRepository.Create<IProcessERP>();
            _mockLogger = _mockRepository.Create<ILogger>();
            _mockEmailNotifier = _mockRepository.Create<IEmailNotifier>();
            _fileFormatSettings = new FileFormatSettings
            {
                Order = new OrderSettings
                {
                    FileTypeIdentifier = new FieldSettings { Start = 0, Length = 3 },
                    OrderNumber = new FieldSettings { Start = 3, Length = 20 },
                    OrderDate = new FieldSettings { Start = 23, Length = 13 },
                    BuyerEAN = new FieldSettings { Start = 36, Length = 13 },
                    SupplierEAN = new FieldSettings { Start = 49, Length = 13 },
                    Comment = new FieldSettings { Start = 62, Length = 100 }
                }, 
                OrderItem = new OrderItemSettings
                {
                    EAN = new FieldSettings { Start = 0, Length = 13 },
                    Description = new FieldSettings { Start = 13, Length = 65 },
                    Quantity = new FieldSettings { Start = 78, Length = 10 },
                    UnitPrice = new FieldSettings { Start = 88, Length = 10 }
                }
            };
            _appSettings = new AppSettings
            {
                AccountManagerEmail = "test@test.com"
            };
        }

        private ProcessOrderService CreateService()
        {
            return new ProcessOrderService(
                _mockLogger.Object,
                _mockProcessERP.Object,
                _fileFormatSettings, 
                _mockEmailNotifier.Object, 
                _appSettings);
        }

        [Test]
        public void IsFileValid_WithoutFileTypeIdentifier_ReturnsFalse()
        {
            // Arrange
            var service = this.CreateService();
            string[] data = ["", "", "", ""];

            // Act
            var result = service.IsFileValid(data);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsFileValid_WithoutData_ReturnsFalse()
        {
            // Arrange
            var service = this.CreateService();
            string[] data = null;

            // Act
            var result = service.IsFileValid(data);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsFileValid_WithOrderData_ReturnsTrue()
        {
            // Arrange
            var service = this.CreateService();
            string[] data = ["ORD12345               20110902T164587123456789378712345678944This is my first order   ", "", "", ""];
            _mockLogger.Setup(x => x.Information(It.IsAny<string>()));

            // Act
            var result = service.IsFileValid(data);

            // Assert
            _mockLogger.Verify(x => x.Information("Data received is an ORDER file, validate successfully"), Times.Once());
            Assert.IsTrue(result);
        }

        [Test]
        public async Task GetOrderFromDataAsync_WhenUnitPriceIsLowerInErp_ThenConsiderUnitPriceOfErp()
        {
            // Arrange
            var service = this.CreateService();
            string[] data = ["ORD12345               20110902T164587123456789378712345678944This is my first order                                                                              ",
                "8712345678906Computer Monitor                                                 12        123.35    ",
                "8712345678913Keyboard                                                         3         56.00     ",
                "8712345678920Mouse                                                            456       12.90     "];

            _mockProcessERP.Setup(x => x.GetItemForSupplier("8712345678906", It.IsAny<string>()))
                .ReturnsAsync(new OrderItemModel
                {
                    EAN = "8712345678906", 
                    Description = "Test Description", 
                    UnitPrice = 120, 
                    Quantity = 500
                });

            _mockProcessERP.Setup(x => x.GetItemForSupplier("8712345678913", It.IsAny<string>()))
                .ReturnsAsync(new OrderItemModel
                {
                    EAN = "8712345678913",
                    Description = "Test Description",
                    UnitPrice = 50m,
                    Quantity = 500
                });

            _mockProcessERP.Setup(x => x.GetItemForSupplier("8712345678920", It.IsAny<string>()))
                .ReturnsAsync(new OrderItemModel
                {
                    EAN = "8712345678920",
                    Description = "Test Description",
                    UnitPrice = 12.90m,
                    Quantity = 500
                });

            _mockProcessERP.Setup(x => x.UpdateStockForItem(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(true);

            _mockLogger.Setup(x => x.Information(It.IsAny<string>()));

            _mockEmailNotifier.Setup(x => x.SendNotificationAsync(It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            // Act
            var result = await service.GetOrderFromDataAsync(
                data);

            // Assert
            _mockLogger.Verify(x => x.Information("Unit Price for Product 8712345678906 Received from ERP 120 is lower than in the file 120 for supplier-8712345678944 "), Times.Once());
            Assert.That(result.OrderItems.Count(), Is.EqualTo(3));
            Assert.That(result.OrderItems.Where(t => t.EAN == "8712345678913").Select(t => t.UnitPrice).First(), Is.EqualTo(50m));
            _mockRepository.VerifyAll();
        }

        [Test]
        public void GetOrderFromDataAsync_WhenDataNotFoundinERP_ThenThrowError()
        {
            // Arrange
            var service = CreateService();
            string[] data = ["ORD12345               20110902T164587123456789378712345678944This is my first order                                                                              ",
                "8712345678906Computer Monitor                                                 12        123.35    ",
                "8712345678913Keyboard                                                         3         56.00     ",
                "8712345678920Mouse                                                            456       12.90     "];

            _mockProcessERP.Setup(x => x.GetItemForSupplier("8712345678906", It.IsAny<string>()))
                .ReturnsAsync((OrderItemModel)null);

            _mockLogger.Setup(x => x.Error(It.IsAny<string>()));

            // Assert
            var result = Assert.ThrowsAsync<InvalidOperationException>(async () =>  await service.GetOrderFromDataAsync(data));

            // Assert
            _mockLogger.Verify(x => x.Error($"Item - 8712345678906 not found in ERP"), Times.Once);
            Assert.That(result.Message, Is.EqualTo("Error in processing data for 8712345678906"));
            _mockRepository.VerifyAll();
        }

        [Test]
        public void GetOrderFromDataAsync_WhenQuantityIsLowerInErp_ThenThrowError()
        {
            // Arrange
            var service = this.CreateService();
            string[] data = ["ORD12345               20110902T164587123456789378712345678944This is my first order                                                                              ",
                "8712345678906Computer Monitor                                                 12        123.35    ",
                "8712345678913Keyboard                                                         3         56.00     ",
                "8712345678920Mouse                                                            456       12.90     "];

            _mockProcessERP.Setup(x => x.GetItemForSupplier("8712345678906", It.IsAny<string>()))
                .ReturnsAsync(new OrderItemModel
                {
                    EAN = "8712345678906",
                    Description = "Test Description",
                    UnitPrice = 120,
                    Quantity = 500
                });

            _mockProcessERP.Setup(x => x.GetItemForSupplier("8712345678913", It.IsAny<string>()))
                .ReturnsAsync(new OrderItemModel
                {
                    EAN = "8712345678913",
                    Description = "Test Description",
                    UnitPrice = 50m,
                    Quantity = 500
                });

            _mockProcessERP.Setup(x => x.GetItemForSupplier("8712345678920", It.IsAny<string>()))
                .ReturnsAsync(new OrderItemModel
                {
                    EAN = "8712345678920",
                    Description = "Test Description",
                    UnitPrice = 12.90m,
                    Quantity = 5
                });

            _mockProcessERP.Setup(x => x.UpdateStockForItem(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(true);

            _mockLogger.Setup(x => x.Information(It.IsAny<string>()));
            _mockLogger.Setup(x => x.Error(It.IsAny<string>()));
            _mockEmailNotifier.Setup(x => x.SendNotificationAsync(It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            // Act
            var result = Assert.ThrowsAsync<InvalidOperationException>(async () => await service.GetOrderFromDataAsync(data));

            // Assert
            _mockLogger.Verify(x => x.Information("Unit Price for Product 8712345678906 Received from ERP 120 is lower than in the file 120 for supplier-8712345678944 "), Times.Once());
            _mockLogger.Verify(x => x.Error("Product 8712345678920 is out of stock. Request QTY: 456, Qty In Stock: 5"), Times.Once());
            Assert.That(result.Message, Is.EqualTo("Error in processing data for 8712345678920"));
            _mockRepository.VerifyAll();
        }


        [Test]
        public async Task ValidateOrderItem_WhenUnitPriceInERPIsLower_ConsiderUnitPriceOfERP()
        {
            // Arrange
            var service = this.CreateService();
            OrderItemModel orderItem = new OrderItemModel
            {
                EAN = "24234",
                Description = "Test Description",
                UnitPrice = 129,
                Quantity = 5
            };
            string? supplierEAN = "test1234";

            _mockProcessERP.Setup(x => x.GetItemForSupplier(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new OrderItemModel
                {
                    EAN = "24234",
                    Description = "Test Description",
                    UnitPrice = 120,
                    Quantity = 500
                });

            _mockLogger.Setup(x => x.Information(It.IsAny<string>()));

            _mockEmailNotifier.Setup(x => x.SendNotificationAsync(It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            // Act
            var result = await service.ValidateOrderItem(
                orderItem,
                supplierEAN);

            // Assert
            _mockLogger.Verify(x => x.Information("Unit Price for Product 24234 Received from ERP 120 is lower than in the file 120 for supplier-test1234 "), Times.Once());
            Assert.IsTrue(result.Item1);
            Assert.That(result.Item2.UnitPrice, Is.EqualTo(120));
            _mockRepository.VerifyAll();
        }

        [Test]
        public async Task ValidateOrderItem_WhenQuantityInERPIsLower_ReturnFalse()
        {
            // Arrange
            var service = this.CreateService();
            OrderItemModel orderItem = new OrderItemModel
            {
                EAN = "24234",
                Description = "Test Description",
                UnitPrice = 129,
                Quantity = 50
            };
            string? supplierEAN = "test1234";

            _mockProcessERP.Setup(x => x.GetItemForSupplier(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new OrderItemModel
                {
                    EAN = "24234",
                    Description = "Test Description",
                    UnitPrice = 120,
                    Quantity = 5
                });

            _mockLogger.Setup(x => x.Information(It.IsAny<string>()));
            _mockLogger.Setup(x => x.Error(It.IsAny<string>()));
            _mockEmailNotifier.Setup(x => x.SendNotificationAsync(It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            // Act
            var result = await service.ValidateOrderItem(
                orderItem,
                supplierEAN);

            // Assert
            _mockLogger.Verify(x => x.Information("Unit Price for Product 24234 Received from ERP 120 is lower than in the file 120 for supplier-test1234 "), Times.Once());
            _mockLogger.Verify(x => x.Error("Product 24234 is out of stock. Request QTY: 50, Qty In Stock: 5"), Times.Once());
            Assert.IsFalse(result.Item1);
            _mockRepository.VerifyAll();
        }

        [Test]
        public void CreateXmlForOrderData_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var service = this.CreateService();
            OrderModel order = new OrderModel
            {
                OrderNumber = "46423", 
                OrderDate = "23-12-2024d", 
                BuyerEAN = "565656", 
                SupplierEAN = "989898", 
                Comment = "this is a test order", 
                OrderItems = [
                    new OrderItemModel
                    {
                        EAN = "12345", 
                        Description = "", 
                        Quantity = 10, 
                        UnitPrice = 20m
                    },
                    new OrderItemModel
                    {
                        EAN = "54321",
                        Description = "",
                        Quantity = 10,
                        UnitPrice = 20m
                    }
               ]
            };
            _mockLogger.Setup(x => x.Information(It.IsAny<string>()));

            // Act
            var result = service.CreateXmlForOrderData(order);

            // Assert
            _mockLogger.Verify(x => x.Information("XML file created successfully"), Times.Once());
            Assert.That(result.Length, Is.GreaterThan(0));
            Assert.IsNotNull(result);
            _mockRepository.VerifyAll();
        }
    }
}
