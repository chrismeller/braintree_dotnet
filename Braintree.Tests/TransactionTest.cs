﻿using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Braintree;
using Braintree.Exceptions;

namespace Braintree.Tests
{
    [TestFixture]
    class TransactionTest
    {
        private BraintreeGateway gateway;

        [SetUp]
        public void Setup()
        {
            gateway = new BraintreeGateway
            {
                Environment = Environment.DEVELOPMENT,
                MerchantId = "integration_merchant_id",
                PublicKey = "integration_public_key",
                PrivateKey = "integration_private_key"
            };
        }

        [Test]
        public void SaleTrData_ReturnsValidTrDataHash()
        {
            String trData = gateway.Transaction.SaleTrData(new TransactionRequest(), "http://example.com");
            Assert.IsTrue(trData.Contains("sale"));
            Assert.IsTrue(TrUtil.IsTrDataValid(trData));
        }

        [Test]
        public void CreditTrData_ReturnsValidTrDataHash()
        {
            String trData = gateway.Transaction.CreditTrData(new TransactionRequest(), "http://example.com");
            Assert.IsTrue(trData.Contains("credit"));
            Assert.IsTrue(TrUtil.IsTrDataValid(trData));
        }

        [Test]
        public void Sale_ReturnsSuccessfulResponse()
        {
            var request = new TransactionRequest
            {
                Amount = SandboxValues.TransactionAmount.AUTHORIZE,
                CreditCard = new CreditCardRequest
                {
                    Number = SandboxValues.CreditCardNumber.VISA,
                    ExpirationDate = "05/2009",
                }
            };

            Result<Transaction> result = gateway.Transaction.Sale(request);
            Assert.IsTrue(result.IsSuccess());
            Transaction transaction = result.Target;

            Assert.AreEqual(1000.00, transaction.Amount);
            Assert.AreEqual(TransactionType.SALE, transaction.Type);
            Assert.AreEqual(TransactionStatus.AUTHORIZED, transaction.Status);
            Assert.AreEqual(DateTime.Now.Year, transaction.CreatedAt.Value.Year);
            Assert.AreEqual(DateTime.Now.Year, transaction.UpdatedAt.Value.Year);

            CreditCard creditCardDetails = transaction.CreditCardDetails;
            Assert.AreEqual("411111", creditCardDetails.Bin);
            Assert.AreEqual("1111", creditCardDetails.LastFour);
            Assert.AreEqual("05", creditCardDetails.ExpirationMonth);
            Assert.AreEqual("2009", creditCardDetails.ExpirationYear);
            Assert.AreEqual("05/2009", creditCardDetails.ExpirationDate);
        }

        [Test]
        public void Sale_WithAllAttributes()
        {
            TransactionRequest request = new TransactionRequest
            {
                Amount = SandboxValues.TransactionAmount.AUTHORIZE,
                OrderId = "123",
                CreditCard = new CreditCardRequest
                {
                    Number = SandboxValues.CreditCardNumber.VISA,
                    CVV = "321",
                    ExpirationDate = "05/2009"
                },
                Customer = new CustomerRequest
                {
                    FirstName = "Dan",
                    LastName = "Smith",
                    Company = "Braintree Payment Solutions",
                    Email = "dan@example.com",
                    Phone = "419-555-1234",
                    Fax = "419-555-1235",
                    Website = "http://braintreepaymentsolutions.com"
                },
                BillingAddress = new AddressRequest
                {
                    FirstName = "Carl",
                    LastName = "Jones",
                    Company = "Braintree",
                    StreetAddress = "123 E Main St",
                    ExtendedAddress = "Suite 403",
                    Locality = "Chicago",
                    Region = "IL",
                    PostalCode = "60622",
                    CountryName = "United States of America",
                },
                ShippingAddress = new AddressRequest
                {
                    FirstName = "Andrew",
                    LastName = "Mason",
                    Company = "Braintree Shipping",
                    StreetAddress = "456 W Main St",
                    ExtendedAddress = "Apt 2F",
                    Locality = "Bartlett",
                    Region = "MA",
                    PostalCode = "60103",
                    CountryName = "Mexico",
                }
            };

            Result<Transaction> result = gateway.Transaction.Sale(request);
            Assert.IsTrue(result.IsSuccess());
            Transaction transaction = result.Target;

            Assert.AreEqual(1000.00, transaction.Amount);
            Assert.AreEqual(TransactionStatus.AUTHORIZED, transaction.Status);
            Assert.AreEqual("123", transaction.OrderId);
            Assert.IsNull(transaction.GetVaultCreditCard());
            Assert.IsNull(transaction.GetVaultCustomer());

            Assert.IsNull(transaction.GetVaultCreditCard());
            CreditCard creditCardDetails = transaction.CreditCardDetails;
            Assert.AreEqual("411111", creditCardDetails.Bin);
            Assert.AreEqual("1111", creditCardDetails.LastFour);
            Assert.AreEqual("05", creditCardDetails.ExpirationMonth);
            Assert.AreEqual("2009", creditCardDetails.ExpirationYear);
            Assert.AreEqual("05/2009", creditCardDetails.ExpirationDate);

            Assert.IsNull(transaction.GetVaultCustomer());
            Customer customer = transaction.CustomerDetails;
            Assert.AreEqual("Dan", customer.FirstName);
            Assert.AreEqual("Smith", customer.LastName);
            Assert.AreEqual("Braintree Payment Solutions", customer.Company);
            Assert.AreEqual("dan@example.com", customer.Email);
            Assert.AreEqual("419-555-1234", customer.Phone);
            Assert.AreEqual("419-555-1235", customer.Fax);
            Assert.AreEqual("http://braintreepaymentsolutions.com", customer.Website);

            Assert.IsNull(transaction.GetVaultBillingAddress());
            Address billingDetails = transaction.BillingAddressDetails;
            Assert.AreEqual("Carl", billingDetails.FirstName);
            Assert.AreEqual("Jones", billingDetails.LastName);
            Assert.AreEqual("Braintree", billingDetails.Company);
            Assert.AreEqual("123 E Main St", billingDetails.StreetAddress);
            Assert.AreEqual("Suite 403", billingDetails.ExtendedAddress);
            Assert.AreEqual("Chicago", billingDetails.Locality);
            Assert.AreEqual("IL", billingDetails.Region);
            Assert.AreEqual("60622", billingDetails.PostalCode);
            Assert.AreEqual("United States of America", billingDetails.CountryName);

            Assert.IsNull(transaction.GetVaultShippingAddress());
            Address shippingDetails = transaction.ShippingAddressDetails;
            Assert.AreEqual("Andrew", shippingDetails.FirstName);
            Assert.AreEqual("Mason", shippingDetails.LastName);
            Assert.AreEqual("Braintree Shipping", shippingDetails.Company);
            Assert.AreEqual("456 W Main St", shippingDetails.StreetAddress);
            Assert.AreEqual("Apt 2F", shippingDetails.ExtendedAddress);
            Assert.AreEqual("Bartlett", shippingDetails.Locality);
            Assert.AreEqual("MA", shippingDetails.Region);
            Assert.AreEqual("60103", shippingDetails.PostalCode);
            Assert.AreEqual("Mexico", shippingDetails.CountryName);
        }

        [Test]
        public void Sale_WithStoreInVaultAndSpecifyingToken()
        {
            String customerId = new Random().Next(1000000).ToString();
            String paymentToken = new Random().Next(1000000).ToString();

            TransactionRequest request = new TransactionRequest
            {
                Amount = SandboxValues.TransactionAmount.AUTHORIZE,
                CreditCard = new CreditCardRequest
                {
                    Token = paymentToken,
                    Number = SandboxValues.CreditCardNumber.VISA,
                    ExpirationDate = "05/2009"
                },
                Customer = new CustomerRequest
                {
                    Id = customerId,
                    FirstName = "Jane",
                },
                Options = new TransactionOptionsRequest
                {
                    StoreInVault = true
                }
            };

            Result<Transaction> result = gateway.Transaction.Sale(request);
            Assert.IsTrue(result.IsSuccess());
            Transaction transaction = result.Target;

            CreditCard creditCardDetails = transaction.CreditCardDetails;
            Assert.AreEqual(paymentToken, creditCardDetails.Token);
            Assert.AreEqual("05/2009", transaction.GetVaultCreditCard().ExpirationDate);

            Customer customer = transaction.CustomerDetails;
            Assert.AreEqual(customerId, customer.Id);
            Assert.AreEqual("Jane", transaction.GetVaultCustomer().FirstName);
        }

        [Test]
        public void Sale_WithStoreInVaultWithoutSpecifyingToken()
        {
            TransactionRequest request = new TransactionRequest
            {
                Amount = SandboxValues.TransactionAmount.AUTHORIZE,
                CreditCard = new CreditCardRequest
                {
                    Number = SandboxValues.CreditCardNumber.VISA,
                    ExpirationDate = "05/2009",
                },
                Customer = new CustomerRequest
                {
                    FirstName = "Jane"
                },
                Options = new TransactionOptionsRequest
                {
                    StoreInVault = true
                }
            };

            Result<Transaction> result = gateway.Transaction.Sale(request);
            Assert.IsTrue(result.IsSuccess());
            Transaction transaction = result.Target;

            CreditCard creditCardDetails = transaction.CreditCardDetails;
            Assert.IsNotNull(creditCardDetails.Token);
            Assert.AreEqual("05/2009", transaction.GetVaultCreditCard().ExpirationDate);

            Customer customerDetails = transaction.CustomerDetails;
            Assert.IsNotNull(customerDetails.Id);
            Assert.AreEqual("Jane", transaction.GetVaultCustomer().FirstName);
        }

        [Test]
        public void Sale_WithStoreInVaultForBillingAndShipping()
        {
            TransactionRequest request = new TransactionRequest
            {
                Amount = SandboxValues.TransactionAmount.AUTHORIZE,
                CreditCard = new CreditCardRequest
                {
                    Number = SandboxValues.CreditCardNumber.VISA,
                    ExpirationDate = "05/2009",
                },
                BillingAddress = new AddressRequest
                {
                    FirstName = "Carl",
                },
                ShippingAddress = new AddressRequest
                {
                    FirstName = "Andrew"
                },
                Options = new TransactionOptionsRequest
                {
                    StoreInVault = true,
                    AddBillingAddressToPaymentMethod = true,
                    StoreShippingAddressInVault = true
                }
            };

            Result<Transaction> result = gateway.Transaction.Sale(request);
            Assert.IsTrue(result.IsSuccess());
            Transaction transaction = result.Target;

            CreditCard creditCard = transaction.GetVaultCreditCard();
            Assert.AreEqual("Carl", creditCard.BillingAddress.FirstName);
            Assert.AreEqual("Carl", transaction.GetVaultBillingAddress().FirstName);
            Assert.AreEqual("Andrew", transaction.GetVaultShippingAddress().FirstName);

            Customer customer = transaction.GetVaultCustomer();
            Assert.AreEqual(2, customer.Addresses.Length);

            List<Address> addresses = new List<Address>(customer.Addresses);
            Assert.AreEqual("Carl", customer.Addresses[0].FirstName);
            Assert.AreEqual("Andrew", customer.Addresses[1].FirstName);

            Assert.IsNotNull(transaction.BillingAddressDetails.Id);
            Assert.IsNotNull(transaction.ShippingAddressDetails.Id);
        }

        [Test]
        public void Sale_Declined()
        {
            TransactionRequest request = new TransactionRequest
            {
                Amount = SandboxValues.TransactionAmount.DECLINE,
                CreditCard = new CreditCardRequest
                {
                    Number = SandboxValues.CreditCardNumber.VISA,
                    ExpirationDate = "05/2009"
                }
            };

            Result<Transaction> result = gateway.Transaction.Sale(request);
            Assert.IsTrue(result.IsSuccess());
            Transaction transaction = result.Target;

            Assert.AreEqual(2000.00, transaction.Amount);
            Assert.AreEqual(TransactionStatus.PROCESSOR_DECLINED, transaction.Status);
            Assert.AreEqual("2000", transaction.ProcessorResponseCode);
            Assert.IsNotNull(transaction.ProcessorResponseText);

            CreditCard creditCardDetails = transaction.CreditCardDetails;
            Assert.AreEqual("411111", creditCardDetails.Bin);
            Assert.AreEqual("1111", creditCardDetails.LastFour);
            Assert.AreEqual("05", creditCardDetails.ExpirationMonth);
            Assert.AreEqual("2009", creditCardDetails.ExpirationYear);
            Assert.AreEqual("05/2009", creditCardDetails.ExpirationDate);
        }

        [Test]
        public void Sale_WithCustomFields()
        {
            TransactionRequest request = new TransactionRequest
            {
                Amount = SandboxValues.TransactionAmount.DECLINE,
                CustomFields = new Dictionary<String, String>
                {
                    { "store_me", "custom value" },
                    { "another_stored_field", "custom value2" }
                },
                CreditCard = new CreditCardRequest
                {
                    Number = SandboxValues.CreditCardNumber.VISA,
                    ExpirationDate = "05/2009"
                }
            };

            Result<Transaction> result = gateway.Transaction.Sale(request);
            Assert.IsTrue(result.IsSuccess());
            Transaction transaction = result.Target;

            Assert.AreEqual("custom value", transaction.CustomFields["store-me"]);
            Assert.AreEqual("custom value2", transaction.CustomFields["another-stored-field"]);
        }

        [Test]
        public void Sale_WithUnregisteredCustomField()
        {
            TransactionRequest request = new TransactionRequest
            {
                Amount = SandboxValues.TransactionAmount.DECLINE,
                CustomFields = new Dictionary<String, String>
                {
                    { "unkown_custom_field", "custom value" }
                },
                CreditCard = new CreditCardRequest
                {
                    Number = SandboxValues.CreditCardNumber.VISA,
                    ExpirationDate = "05/2009"
                }
            };

            Result<Transaction> result = gateway.Transaction.Sale(request);
            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(ValidationErrorCode.TRANSACTION_CUSTOM_FIELD_IS_INVALID, result.Errors.ForObject("transaction").OnField("custom_fields")[0].Code);
        }

        [Test]
        public void Sale_WithPaymentMethodToken()
        {
            Customer customer = gateway.Customer.Create(new CustomerRequest()).Target;
            CreditCardRequest creditCardRequest = new CreditCardRequest
            {
                CustomerId = customer.Id,
                CVV = "123",
                Number = "5105105105105100",
                ExpirationDate = "05/12"
            };

            CreditCard creditCard = gateway.CreditCard.Create(creditCardRequest).Target;

            TransactionRequest request = new TransactionRequest
            {
                Amount = SandboxValues.TransactionAmount.AUTHORIZE,
                PaymentMethodToken = creditCard.Token
            };

            Result<Transaction> result = gateway.Transaction.Sale(request);
            Assert.IsTrue(result.IsSuccess());
            Transaction transaction = result.Target;

            Assert.AreEqual(creditCard.Token, transaction.CreditCardDetails.Token);
            Assert.AreEqual("510510", transaction.CreditCardDetails.Bin);
            Assert.AreEqual("05/2012", transaction.CreditCardDetails.ExpirationDate);
        }

        [Test]
        public void Sale_UsesShippingAddressFromVault()
        {
            Customer customer = gateway.Customer.Create(new CustomerRequest()).Target;

            gateway.CreditCard.Create(new CreditCardRequest
            {
                CustomerId = customer.Id,
                CVV = "123",
                Number = "5105105105105100",
                ExpirationDate = "05/12"
            });

            Address shippingAddress = gateway.Address.Create(customer.Id, new AddressRequest { FirstName = "Carl" }).Target;

            TransactionRequest request = new TransactionRequest
            {
                Amount = SandboxValues.TransactionAmount.AUTHORIZE,
                CustomerId = customer.Id,
                ShippingAddressId = shippingAddress.Id
            };

            Result<Transaction> result = gateway.Transaction.Sale(request);
            Assert.IsTrue(result.IsSuccess());
            Transaction transaction = result.Target;

            Assert.AreEqual(shippingAddress.Id, transaction.ShippingAddressDetails.Id);
            Assert.AreEqual("Carl", transaction.ShippingAddressDetails.FirstName);
        }

        [Test]
        public void Sale_WithValidationError()
        {
            TransactionRequest request = new TransactionRequest
            {
                CreditCard = new CreditCardRequest
                {
                    ExpirationMonth = "05",
                    ExpirationYear = "2010"
                }
            };

            Result<Transaction> result = gateway.Transaction.Sale(request);
            Assert.IsFalse(result.IsSuccess());
            Assert.IsNull(result.Target);

            Assert.AreEqual(ValidationErrorCode.TRANSACTION_AMOUNT_IS_REQUIRED, result.Errors.ForObject("transaction").OnField("amount")[0].Code);
            Dictionary<String, String> parameters = result.Parameters;
            Assert.IsFalse(parameters.ContainsKey("transaction[amount]"));
            Assert.AreEqual("05", parameters["transaction[credit_card][expiration_month]"]);
            Assert.AreEqual("2010", parameters["transaction[credit_card][expiration_year]"]);
        }

        [Test]
        public void ConfirmTransparentRedirect_CreatesTheTransaction()
        {
            TransactionRequest trParams = new TransactionRequest
            {
                Type = TransactionType.SALE
            };

            TransactionRequest request = new TransactionRequest
            {
                Amount = SandboxValues.TransactionAmount.AUTHORIZE,
                CreditCard = new CreditCardRequest
                {
                    Number = SandboxValues.CreditCardNumber.VISA,
                    ExpirationDate = "05/2009",
                }
            };

            String queryString = TestHelper.QueryStringForTR(trParams, request, gateway.Transaction.TransparentRedirectURLForCreate());
            Result<Transaction> result = gateway.Transaction.ConfirmTransparentRedirect(queryString);
            Assert.IsTrue(result.IsSuccess());
            Transaction transaction = result.Target;

            Assert.AreEqual(1000.00, transaction.Amount);
            Assert.AreEqual(TransactionType.SALE, transaction.Type);
            Assert.AreEqual(TransactionStatus.AUTHORIZED, transaction.Status);
            Assert.AreEqual(DateTime.Now.Year, transaction.CreatedAt.Value.Year);
            Assert.AreEqual(DateTime.Now.Year, transaction.UpdatedAt.Value.Year);

            CreditCard creditCardDetails = transaction.CreditCardDetails;
            Assert.AreEqual("411111", creditCardDetails.Bin);
            Assert.AreEqual("1111", creditCardDetails.LastFour);
            Assert.AreEqual("05", creditCardDetails.ExpirationMonth);
            Assert.AreEqual("2009", creditCardDetails.ExpirationYear);
            Assert.AreEqual("05/2009", creditCardDetails.ExpirationDate);
        }

        [Test]
        public void Credit_WithValidParams()
        {
            TransactionRequest request = new TransactionRequest
            {
                Amount = SandboxValues.TransactionAmount.AUTHORIZE,
                CreditCard = new CreditCardRequest
                {
                    Number = SandboxValues.CreditCardNumber.VISA,
                    ExpirationDate = "05/2009"
                }
            };

            Result<Transaction> result = gateway.Transaction.Credit(request);
            Assert.IsTrue(result.IsSuccess());
            Transaction transaction = result.Target;

            Assert.AreEqual(1000.00, transaction.Amount);
            Assert.AreEqual(TransactionType.CREDIT, transaction.Type);
            Assert.AreEqual(TransactionStatus.SUBMITTED_FOR_SETTLEMENT, transaction.Status);

            CreditCard creditCard = transaction.CreditCardDetails;
            Assert.AreEqual("411111", creditCard.Bin);
            Assert.AreEqual("1111", creditCard.LastFour);
            Assert.AreEqual("05", creditCard.ExpirationMonth);
            Assert.AreEqual("2009", creditCard.ExpirationYear);
            Assert.AreEqual("05/2009", creditCard.ExpirationDate);
        }

        [Test]
        public void Credit_WithCustomFields()
        {
            TransactionRequest request = new TransactionRequest
            {
                Amount = SandboxValues.TransactionAmount.DECLINE,
                CustomFields = new Dictionary<String, String>
                {
                    { "store_me", "custom value"},
                    { "another_stored_field", "custom value2" }
                },
                CreditCard = new CreditCardRequest
                {
                    Number = SandboxValues.CreditCardNumber.VISA,
                    ExpirationDate = "05/2009"
                }
            };

            Result<Transaction> result = gateway.Transaction.Credit(request);
            Assert.IsTrue(result.IsSuccess());
            Transaction transaction = result.Target;

            Assert.AreEqual("custom value", transaction.CustomFields["store-me"]);
            Assert.AreEqual("custom value2", transaction.CustomFields["another-stored-field"]);
        }

        [Test]
        public void Credit_WithValidationError()
        {
            TransactionRequest request = new TransactionRequest
            {
                CreditCard = new CreditCardRequest
                {
                    ExpirationMonth = "05",
                    ExpirationYear = "2010"
                }
            };

            Result<Transaction> result = gateway.Transaction.Credit(request);
            Assert.IsFalse(result.IsSuccess());
            Assert.IsNull(result.Target);

            Assert.AreEqual(ValidationErrorCode.TRANSACTION_AMOUNT_IS_REQUIRED, result.Errors.ForObject("transaction").OnField("amount")[0].Code);

            Dictionary<String, String> parameters = result.Parameters;
            Assert.IsFalse(parameters.ContainsKey("transaction[amount]"));
            Assert.AreEqual("05", parameters["transaction[credit_card][expiration_month]"]);
            Assert.AreEqual("2010", parameters["transaction[credit_card][expiration_year]"]);
        }

        [Test]
        public void Find_WithAValidTransactionId()
        {
            TransactionRequest request = new TransactionRequest
            {
                Amount = SandboxValues.TransactionAmount.AUTHORIZE,
                CreditCard = new CreditCardRequest
                {
                    Number = SandboxValues.CreditCardNumber.VISA,
                    ExpirationDate = "05/2008"
                }
            };

            Transaction transaction = gateway.Transaction.Sale(request).Target;

            Transaction foundTransaction = gateway.Transaction.Find(transaction.Id);

            Assert.AreEqual(transaction.Id, foundTransaction.Id);
            Assert.AreEqual(TransactionStatus.AUTHORIZED, foundTransaction.Status);
            Assert.AreEqual("05/2008", foundTransaction.CreditCardDetails.ExpirationDate);
        }

        [Test]
        public void Find_WithBadId()
        {
            Assert.Throws<NotFoundException>(() => gateway.Transaction.Find("badId"));
        }

        [Test]
        public void Void_VoidsTheTransaction()
        {
            TransactionRequest request = new TransactionRequest
            {
                Amount = SandboxValues.TransactionAmount.AUTHORIZE,
                CreditCard = new CreditCardRequest
                {
                    Number = SandboxValues.CreditCardNumber.VISA,
                    ExpirationDate = "05/2008"
                }
            };

            Transaction transaction = gateway.Transaction.Sale(request).Target;
            Result<Transaction> result = gateway.Transaction.Void(transaction.Id);
            Assert.IsTrue(result.IsSuccess());

            Assert.AreEqual(transaction.Id, result.Target.Id);
            Assert.AreEqual(TransactionStatus.VOIDED, result.Target.Status);
        }

        [Test]
        public void Void_WithBadId()
        {
            Assert.Throws<NotFoundException>(() => gateway.Transaction.Void("badId"));
        }

        [Test]
        public void Void_WithValidationError()
        {
            TransactionRequest request = new TransactionRequest
            {
                Amount = SandboxValues.TransactionAmount.AUTHORIZE,
                CreditCard = new CreditCardRequest
                {
                    Number = SandboxValues.CreditCardNumber.VISA,
                    ExpirationDate = "05/2008"
                }
            };

            Transaction transaction = gateway.Transaction.Sale(request).Target;
            Result<Transaction> result = gateway.Transaction.Void(transaction.Id);
            Assert.IsTrue(result.IsSuccess());

            result = gateway.Transaction.Void(transaction.Id);
            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(ValidationErrorCode.TRANSACTION_CANNOT_BE_VOIDED, result.Errors.ForObject("transaction").OnField("base")[0].Code);
        }

        [Test]
        public void SubmitForSettlement_WithoutAmount()
        {
            TransactionRequest request = new TransactionRequest
            {
                Amount = SandboxValues.TransactionAmount.AUTHORIZE,
                CreditCard = new CreditCardRequest
                {
                    Number = SandboxValues.CreditCardNumber.VISA,
                    ExpirationDate = "05/2008"
                }
            };

            Transaction transaction = gateway.Transaction.Sale(request).Target;

            Result<Transaction> result = gateway.Transaction.SubmitForSettlement(transaction.Id);

            Assert.IsTrue(result.IsSuccess());
            Assert.AreEqual(TransactionStatus.SUBMITTED_FOR_SETTLEMENT, result.Target.Status);
            Assert.AreEqual(SandboxValues.TransactionAmount.AUTHORIZE, result.Target.Amount);
        }

        [Test]
        public void SubmitForSettlement_WithAmount()
        {
            TransactionRequest request = new TransactionRequest
            {
                Amount = SandboxValues.TransactionAmount.AUTHORIZE,
                CreditCard = new CreditCardRequest
                {
                    Number = SandboxValues.CreditCardNumber.VISA,
                    ExpirationDate = "05/2008"
                }
            };

            Transaction transaction = gateway.Transaction.Sale(request).Target;
            Result<Transaction> result = gateway.Transaction.SubmitForSettlement(transaction.Id, Decimal.Parse("50.00"));

            Assert.IsTrue(result.IsSuccess());
            Assert.AreEqual(TransactionStatus.SUBMITTED_FOR_SETTLEMENT, result.Target.Status);
            Assert.AreEqual(50.00, result.Target.Amount);
        }

        [Test]
        public void SubmitForSettlement_WithValidationError()
        {
            TransactionRequest request = new TransactionRequest
            {
                Amount = SandboxValues.TransactionAmount.AUTHORIZE,
                CreditCard = new CreditCardRequest
                {
                    Number = SandboxValues.CreditCardNumber.VISA,
                    ExpirationDate = "05/2008"
                }
            };

            Transaction transaction = gateway.Transaction.Sale(request).Target;
            Result<Transaction> result = gateway.Transaction.SubmitForSettlement(transaction.Id);
            Assert.IsTrue(result.IsSuccess());

            result = gateway.Transaction.SubmitForSettlement(transaction.Id);
            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(ValidationErrorCode.TRANSACTION_CANNOT_SUBMIT_FOR_SETTLEMENT, result.Errors.ForObject("transaction").OnField("base")[0].Code);
        }

        [Test]
        public void SubmitForSettlement_WithBadId()
        {
            Assert.Throws<NotFoundException>(() => gateway.Transaction.SubmitForSettlement("badId"));
        }

        [Test]
        public void Search_WithMatches()
        {
            PagedCollection pagedCollection = gateway.Transaction.Search("411111");

            Assert.IsTrue(pagedCollection.TotalItems > 0);
            Assert.IsTrue(pagedCollection.PageSize > 0);
            Assert.AreEqual(1, pagedCollection.CurrentPageNumber);
            Assert.AreEqual("411111", pagedCollection.Transactions[0].CreditCardDetails.Bin);
        }

        [Test]
        public void Search_WithPageNumber()
        {
            PagedCollection pagedCollection = gateway.Transaction.Search("411111", 2);
            Assert.AreEqual(2, pagedCollection.CurrentPageNumber);
        }

        [Test]
        public void Search_CanTraversePages()
        {
            PagedCollection pagedCollection = gateway.Transaction.Search("411111");
            Assert.AreEqual(1, pagedCollection.CurrentPageNumber);

            PagedCollection nextPage = pagedCollection.GetNextPage();
            Assert.AreEqual(2, nextPage.CurrentPageNumber);
            Assert.AreNotEqual(pagedCollection.Transactions[0].Id, nextPage.Transactions[0].Id);
        }

        [Test]
        public void Refund_WithABasicTransaction()
        {
            TransactionRequest request = new TransactionRequest
            {
                Amount = SandboxValues.TransactionAmount.AUTHORIZE,
                CreditCard = new CreditCardRequest
                {
                    Number = SandboxValues.CreditCardNumber.VISA,
                    ExpirationDate = "05/2008"
                },
                Options = new TransactionOptionsRequest
                {
                    SubmitForSettlement = true
                }
            };

            Transaction transaction = gateway.Transaction.Sale(request).Target;
            Settle(transaction.Id);

            Result<Transaction> result = gateway.Transaction.Refund(transaction.Id);
            Assert.IsTrue(result.IsSuccess());
            Assert.AreEqual(TransactionType.CREDIT, result.Target.Type);
            Assert.AreEqual(transaction.Amount, result.Target.Amount);
        }

        private void Settle(String transactionId)
        {
            NodeWrapper response = new NodeWrapper(WebServiceGateway.Put("/transactions/" + transactionId + "/settle"));
            Assert.IsTrue(response.IsSuccess());
        }

        [Test]
        public void Settle_RefundFailsWithNonSettledTransaction()
        {
            TransactionRequest request = new TransactionRequest
            {
                Amount = SandboxValues.TransactionAmount.AUTHORIZE,
                CreditCard = new CreditCardRequest
                {
                    Number = SandboxValues.CreditCardNumber.VISA,
                    ExpirationDate = "05/2008"
                }
            };

            Transaction transaction = gateway.Transaction.Sale(request).Target;
            Assert.AreEqual(TransactionStatus.AUTHORIZED, transaction.Status);

            Result<Transaction> result = gateway.Transaction.Refund(transaction.Id);
            Assert.IsFalse(result.IsSuccess());

            Assert.AreEqual(ValidationErrorCode.TRANSACTION_CANNOT_REFUND_UNLESS_SETTLED, result.Errors.ForObject("transaction").OnField("base")[0].Code);
        }
    }
}