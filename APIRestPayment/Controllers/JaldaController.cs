﻿using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace APIRestPayment.Controllers
{
    public class JaldaController : BaseApiController
    {
        #region Handlers
        CASPaymentDAO.DataHandler.JaldaContractDataHandler jaldaContractHandler = new CASPaymentDAO.DataHandler.JaldaContractDataHandler(WebApiApplication.SessionFactory);
        CASPaymentDAO.DataHandler.JaldaTicksDataHandler jaldaThicksHandler = new CASPaymentDAO.DataHandler.JaldaTicksDataHandler(WebApiApplication.SessionFactory);
        CASPaymentDAO.DataHandler.AccountDataHandler accountHandler = new CASPaymentDAO.DataHandler.AccountDataHandler(WebApiApplication.SessionFactory);
        CASPaymentDAO.DataHandler.TransactionTypeDataHandler transactionTypeHandler = new CASPaymentDAO.DataHandler.TransactionTypeDataHandler(WebApiApplication.SessionFactory);
        #endregion

        #region POST Methods

        //public HttpResponseMessage Post([FromBody] APIRestPayment.Models.POSTModels.JaldaContractPOSTModel jaldaPOSTModel)
        //{
        //    try
        //    {
        //        string ParseErrorMessage;
        //        CASPaymentDTO.Domain.JaldaContract jaldaContract = TheModelFactory.Parse(jaldaPOSTModel, out ParseErrorMessage);

        //        if (jaldaContract == null)
        //        {
        //            return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
        //            {
        //                meta = new Models.MetaModel
        //                {
        //                    code = (int)HttpStatusCode.BadRequest,
        //                    errorMessage = ParseErrorMessage,
        //                }
        //            });
        //        }
        //        //////////////////
        //        //check the validity of this post. check jalda request parameters.                
        //        if (CheckValidityofPost(jaldaContract))
        //        {
        //            //Complete unassigned data in account

        //            account.IsActive = true;
        //            account.Paymentcode = this.GeneratePaymentCode();
        //            account.Accountnumber = this.GenerateAccountNumber(account);
        //            account.Balance = 0;
        //            account.Dateofopening = DateTime.Now;

        //            /////////////////
        //            ///Here we start a transaction to save the account
        //            try
        //            {
        //                accountHandler.Save(account);
        //                //Return account with details including its Id
        //                account = accountHandler.Search(account).Cast<CASPaymentDTO.Domain.Account>().First();
        //                if (account != null)
        //                {
        //                    var result = Request.CreateResponse(HttpStatusCode.Created, new Models.QueryResponseModel
        //                    {
        //                        meta = new Models.MetaModel
        //                        {
        //                            code = (int)HttpStatusCode.Created,
        //                        },
        //                        data = this.TheModelFactory.Create(account, DataAccessTypes.Owner),
        //                    });
        //                    return result;
        //                }


        //            }
        //            catch (TransactionAbortedException ex)
        //            {
        //                return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
        //                {
        //                    meta = new Models.MetaModel
        //                    {
        //                        code = (int)HttpStatusCode.BadRequest,
        //                        errorMessage = "Could not save the account.\n" + ex
        //                    },
        //                });
        //            }
        //            catch (ApplicationException ex)
        //            {
        //                return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
        //                {
        //                    meta = new Models.MetaModel
        //                    {
        //                        code = (int)HttpStatusCode.BadRequest,
        //                        errorMessage = "Some problems happend while saving the account. Account not saved!\n" + ex
        //                    },
        //                });
        //            }
        //        }
        //        else
        //        {
        //            return Request.CreateResponse(HttpStatusCode.Unauthorized, new Models.QueryResponseModel
        //            {
        //                meta = new Models.MetaModel
        //                {
        //                    code = (int)HttpStatusCode.Unauthorized,
        //                    errorMessage = "You are not allowed to create accounts for other users!"
        //                },
        //            });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
        //        {
        //            meta = new Models.MetaModel
        //            {
        //                code = (int)HttpStatusCode.BadRequest,
        //                errorMessage = "The account cannot be saved:\n" + ex
        //            },
        //        });
        //    }
        //    return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
        //    {
        //        meta = new Models.MetaModel
        //        {
        //            code = (int)HttpStatusCode.BadRequest,
        //            errorMessage = "Account not saved due to problems\n"
        //        },
        //    });
        //}

        public HttpResponseMessage Post([FromBody] APIRestPayment.Models.POSTModels.JaldaThickPOSTModel jaldaThickPOSTModel)
        {
            CASPaymentDTO.Domain.JaldaContract jaldaContract;
            string ErrorMessage = "";
            string BodyMessage = "";
            string BodyTrailer = (jaldaThickPOSTModel.SerialNumber != null) ? ("Response to serial:" + jaldaThickPOSTModel.SerialNumber + " \r\n OrderNumber:" + jaldaThickPOSTModel.OrderNumber) : "-";
            bool? terminateContract;
            if (CheckValidityofPost(jaldaThickPOSTModel, out jaldaContract, out ErrorMessage, out terminateContract))
            {
                bool HasError = false;
                if (jaldaThickPOSTModel.JaldaThickType == Constants.JaldaThickTypes.Payment)
                {
                    CASPaymentDTO.Domain.JaldaTicks jaldaThick;
                    HasError = PerformPaymentThick(jaldaThickPOSTModel, out jaldaThick, out BodyMessage, out ErrorMessage);
                    if (!HasError)
                    {
                        //1 send Payment message
                        return Request.CreateResponse(HttpStatusCode.OK, new Models.QueryResponseModel
                        {
                            meta = new Models.MetaModel
                            {
                                code = (int)HttpStatusCode.OK,
                            },
                            data = Newtonsoft.Json.JsonConvert.SerializeObject(new object[] { BodyMessage + "\r\n" + BodyTrailer, this.TheModelFactory.Create(jaldaThick) }),
                        });
                    }
                }
                else if (jaldaThickPOSTModel.JaldaThickType == Constants.JaldaThickTypes.Start)
                {
                    HasError = PerformStartThick(jaldaThickPOSTModel, jaldaContract, out BodyMessage, out ErrorMessage);
                    if (!HasError)
                    {
                        //1 send start message
                        return Request.CreateResponse(HttpStatusCode.OK, new Models.QueryResponseModel
                        {
                            meta = new Models.MetaModel
                            {
                                code = (int)HttpStatusCode.OK,
                            },
                            data = BodyMessage + "\r\n" + BodyTrailer,
                        });
                    }

                }
                else if (jaldaThickPOSTModel.JaldaThickType == Constants.JaldaThickTypes.Terminate)
                {
                    //1) Do Termination
                    HasError = PerformTerminateThick(jaldaThickPOSTModel, jaldaContract, out BodyMessage, out ErrorMessage);
                    if (!HasError)
                    {
                        //2 send terminated message
                        return Request.CreateResponse(HttpStatusCode.OK, new Models.QueryResponseModel
                        {
                            meta = new Models.MetaModel
                            {
                                code = (int)HttpStatusCode.OK,
                            },
                            data = BodyMessage + "\r\n" + BodyTrailer,
                        });
                    }
                }
                //////////////////Begin Error Sending
                if (HasError)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
                    {
                        meta = new Models.MetaModel
                        {
                            code = (int)HttpStatusCode.BadRequest,
                            errorMessage = ErrorMessage,
                        },
                        data = BodyMessage + "\r\n" + BodyTrailer,
                    });
                }
                //////////////////End Error Sending
            }
            else
            {
                if ((bool)terminateContract)
                {
                    bool hasErrors = PerformTerminateThick(jaldaThickPOSTModel, jaldaContract, out BodyMessage, out ErrorMessage);
                }

                return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
                {
                    meta = new Models.MetaModel
                    {
                        code = (int)HttpStatusCode.BadRequest,
                        errorMessage = ErrorMessage,
                    },
                    data = BodyMessage + "\r\n" + BodyTrailer,
                });
            }

            return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
            {
                meta = new Models.MetaModel
                {
                    code = (int)HttpStatusCode.BadRequest,
                    errorMessage = ErrorMessage,
                },
                data = "Process Failed due to unknown problems. " + "\r\n" + BodyTrailer,
            });

        }

        #endregion

        #region Jalda Thick Operations (Start - Payment - Terminate)
        private bool PerformTerminateThick(Models.POSTModels.JaldaThickPOSTModel jaldaThickPOSTModel, CASPaymentDTO.Domain.JaldaContract jaldaContract, out string BodyMessage, out string ErrorMessage)
        {
            //1) set initial Serial Number and isStarted
            jaldaContract.Isterminated = true;

            //2) subtract deposit from Account. Deposit amount = maxAmount in contract. Before performing deposit we must ensure payer has enough credit in his/her account.
            PaymentsController paymentController = new PaymentsController();

            //3 create deposit and jalda transactions
            CASPaymentDTO.Domain.Transactions ReverseDepositPayerTransaction = GenerateDepositTransaction(jaldaContract, true);
            CASPaymentDTO.Domain.Transactions JaldaPayerTransaction = GenerateJaldaTransactions(jaldaContract);
            CASPaymentDTO.Domain.JaldaTicks jaldaThick = this.TheModelFactory.Parse(jaldaThickPOSTModel, out ErrorMessage);
            jaldaThick.Type = Constants.JaldaThickTypes.Terminate;
            if (ReverseDepositPayerTransaction != null)
            {
                using (var session = WebApiApplication.SessionFactory.OpenSession())
                using (var tx = session.BeginTransaction())
                {
                    try
                    {
                        //3 Pay the deposit back                                      
                        paymentController.PerformPayment(ReverseDepositPayerTransaction);
                        //4 Pay Jalda Transaction to payee
                        if (JaldaPayerTransaction.Amount != 0)
                        {
                            paymentController.PerformPayment(JaldaPayerTransaction);
                        }

                        //5 save Terminate thick
                        jaldaThicksHandler.Save(jaldaThick);

                        tx.Commit();

                        //5) set responce to TERMINATED
                        BodyMessage = "TERMINATED";

                        return false;
                    }
                    catch (NHibernate.StaleStateException ex)
                    {
                        BodyMessage = "Error";
                        ErrorMessage = "Problem in clearing jalda payments";
                        tx.Rollback();
                        return true;
                    }
                }
            }
            BodyMessage = "Error";
            ErrorMessage = "Could not create reverse deposit";
            return true;
        }
        private bool PerformPaymentThick(Models.POSTModels.JaldaThickPOSTModel jaldaThickPOSTModel, out CASPaymentDTO.Domain.JaldaTicks jaldaThick, out string BodyMessage, out string ErrorMessage)
        {
            jaldaThick = this.TheModelFactory.Parse(jaldaThickPOSTModel, out ErrorMessage);
            using (var session = WebApiApplication.SessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                try
                {
                    //1 save jalda thick
                    jaldaThicksHandler.Save(jaldaThick);

                    tx.Commit();

                    //2) set responce to STARTED
                    BodyMessage = "PAID";

                    return false;
                }
                catch (NHibernate.StaleStateException ex)
                {
                    BodyMessage = "Error";
                    ErrorMessage = "Problem in saving thick";
                    tx.Rollback();
                    return true;
                }
            }
        }
        private bool PerformStartThick(Models.POSTModels.JaldaThickPOSTModel jaldaThickPOSTModel, CASPaymentDTO.Domain.JaldaContract jaldaContract, out string BodyMessage, out string ErrorMessage)
        {
            //1) set initial Serial Number and isStarted
            jaldaContract.Startserialnumber = (int)jaldaThickPOSTModel.SerialNumber;
            jaldaContract.IsStarted = true;

            //2) subtract deposit from Account. Deposit amount = maxAmount in contract. Before performing deposit we must ensure payer has enough credit in his/her account.
            PaymentsController paymentController = new PaymentsController();
            //Caution: Deadlock may happen

            //bool hasEnoughMoney =paymentController.CheckBalance(jaldaContract.SourceAccountItem, jaldaContract.Maxamount).Result;
            bool hasEnoughMoney = AsyncContext.Run(() => paymentController.CheckBalance(jaldaContract.SourceAccountItem, jaldaContract.Maxamount));
            if (hasEnoughMoney)
            {
                CASPaymentDTO.Domain.Transactions DepositPayerAccount = GenerateDepositTransaction(jaldaContract, false);
                if (DepositPayerAccount != null)
                {
                    string paymentResult = paymentController.PerformPayment(DepositPayerAccount);
                    if (paymentResult == "Success")
                    {
                        CASPaymentDTO.Domain.JaldaTicks jaldaThick = this.TheModelFactory.Parse(jaldaThickPOSTModel, out ErrorMessage);
                        using (var session = WebApiApplication.SessionFactory.OpenSession())
                        using (var tx = session.BeginTransaction())
                        {
                            try
                            {
                                //3 update jaldacontract                                      
                                jaldaContractHandler.Update(jaldaContract);

                                //4 save jalda thick
                                jaldaThicksHandler.Save(jaldaThick);

                                tx.Commit();

                                //5) set responce to STARTED
                                BodyMessage = "STARTED";
                                return false;
                            }
                            catch (NHibernate.StaleStateException ex)
                            {
                                BodyMessage = "Error";
                                ErrorMessage = "Problem in updating contract";
                                tx.Rollback();
                                return true;
                            }
                        }
                    }
                    else
                    {
                        BodyMessage = "Error";
                        ErrorMessage = "Problem in depositing";
                        return true;
                    }

                }
                else
                {
                    BodyMessage = "Error";
                    ErrorMessage = "Problem in creating deposit transaction";
                    return true;
                }
            }
            else
            {
                BodyMessage = "Error";
                ErrorMessage = "Payer does not have enough money";
                return true;
            }
        }

        #endregion

        #region Transaction Generation

        private CASPaymentDTO.Domain.Transactions GenerateJaldaTransactions(CASPaymentDTO.Domain.JaldaContract jaldaContract)
        {
            CASPaymentDTO.Domain.Transactions JaldaTransaction = new CASPaymentDTO.Domain.Transactions();
            //Jalda : charging user

            //acount of customer
            JaldaTransaction.DestinationAccountItem = jaldaContract.SourceAccountItem;
            //acount of client (web site)
            JaldaTransaction.SourceAccountItem = jaldaContract.DestinationAccountItem;
            Decimal numberOfPaymentThicks = jaldaContract.JaldaTicksS.Where(x => x.Type == Constants.JaldaThickTypes.Payment).Count();
            JaldaTransaction.Amount = (-1) * Math.Abs(numberOfPaymentThicks) * jaldaContract.Pricepertick;
            CASPaymentDTO.Domain.TransactionType transactionTypeItem = transactionTypeHandler.Search(new CASPaymentDTO.Domain.TransactionType { NameEn = Constants.TransactionTypes.Jalda }).Cast<CASPaymentDTO.Domain.TransactionType>().FirstOrDefault();
            JaldaTransaction.CurrencyTypeItem = jaldaContract.CurrencyTypeItem;
            JaldaTransaction.TransactionTypeItem = transactionTypeItem;
            //////
            JaldaTransaction.Description = "Jalda Payment for " + numberOfPaymentThicks + " thicks received. Each thick charged payer " + jaldaContract.Pricepertick + " " + jaldaContract.CurrencyTypeItem.Charcode;
            return JaldaTransaction;
        }

        private CASPaymentDTO.Domain.Transactions GenerateDepositTransaction(CASPaymentDTO.Domain.JaldaContract jaldaContract, bool isReverseDeposit)
        {
            CASPaymentDTO.Domain.Transactions depositTransaction = new CASPaymentDTO.Domain.Transactions();

            CASPaymentDTO.Domain.Account accSystem = accountHandler.Search(new CASPaymentDTO.Domain.Account { Accountnumber = Constants.PaymentSystemConstants.TemporaryAccountNumber }).Cast<CASPaymentDTO.Domain.Account>().FirstOrDefault();
            if (!object.Equals(accSystem, default(CASPaymentDTO.Domain.Account)))
            {
                if (!isReverseDeposit)
                {
                    //Deposit : charging user

                    //acount of customer
                    depositTransaction.DestinationAccountItem = jaldaContract.SourceAccountItem;
                    //acount of paymentSystem
                    depositTransaction.SourceAccountItem = accSystem;
                    depositTransaction.Description = "Charging payer for Jalda contract registered with ordernumber: " + jaldaContract.Ordernumber;
                }
                else
                {
                    //Deposit : Paying back the deposit

                    //acount of customer id paid
                    depositTransaction.SourceAccountItem = jaldaContract.SourceAccountItem;
                    //acount of paymentSystem pays
                    depositTransaction.DestinationAccountItem = accSystem;
                    depositTransaction.Description = "Paying back the deposit for Jalda contract registered with ordernumber: " + jaldaContract.Ordernumber;
                }
                depositTransaction.Amount = (-1) * Math.Abs((Decimal)jaldaContract.Maxamount);
                CASPaymentDTO.Domain.TransactionType transactionTypeItem = transactionTypeHandler.Search(new CASPaymentDTO.Domain.TransactionType { NameEn = Constants.TransactionTypes.Deposit }).Cast<CASPaymentDTO.Domain.TransactionType>().FirstOrDefault();
                depositTransaction.CurrencyTypeItem = jaldaContract.CurrencyTypeItem;
                depositTransaction.TransactionTypeItem = transactionTypeItem;
            }
            else return null;

            return depositTransaction;
        }

        #endregion

        #region Checks

        private bool CheckValidityofPost(Models.POSTModels.JaldaThickPOSTModel jaldaThickPOSTModel, out CASPaymentDTO.Domain.JaldaContract relativeJaldaContract, out string ErrorMessage, out bool? terminateContract)
        {
            bool result = true;
            if (string.IsNullOrEmpty(jaldaThickPOSTModel.JaldaThickType) || jaldaThickPOSTModel.SerialNumber == null || jaldaThickPOSTModel.JaldaContractID == null)
            {
                terminateContract = false;
                relativeJaldaContract = null;
                ErrorMessage = "Null values in request";
                return false;
            }
            try
            {
                relativeJaldaContract = jaldaContractHandler.GetEntity((long)jaldaThickPOSTModel.JaldaContractID);

                ///////////////////////////////////////////////

                //Check if contract is already terminated
                if (relativeJaldaContract.Isterminated)
                {
                    ErrorMessage = "Jalda contract is already terminated.";
                    terminateContract = false;
                    return false;
                }
                //Check if contract is started
                else if (jaldaThickPOSTModel.JaldaThickType != Constants.JaldaThickTypes.Start && !relativeJaldaContract.IsStarted)
                {
                    ErrorMessage = "Jalda contract is not started. Send a thick with START type containing initial serial number and other attributes.";
                    terminateContract = false;
                    return false;
                }
                //Duplicate Start Thicks
                else if (jaldaThickPOSTModel.JaldaThickType == Constants.JaldaThickTypes.Start && relativeJaldaContract.IsStarted)
                {
                    ErrorMessage = "Contract is already started. Duplicate START thicks received.";
                    terminateContract = false;
                    return false;
                }
                //Duplicate Terminate Thicks
                else if (jaldaThickPOSTModel.JaldaThickType == Constants.JaldaThickTypes.Terminate && relativeJaldaContract.Isterminated)
                {
                    ErrorMessage = "Contract is already terminated. Duplicate TERMINATE thicks received.";
                    terminateContract = false;
                    return false;
                }
                //////////////////////////////////////////////////


                //Check serial number integrity
                foreach (var item in relativeJaldaContract.JaldaTicksS)
                {
                    if (item.Serialnumber == jaldaThickPOSTModel.SerialNumber)
                    {
                        ErrorMessage = "Jalda Thick with serial number " + item.Serialnumber + " is already sent. Risk of replay attack.";
                        terminateContract = true;
                        return false;
                    }

                }
                if (relativeJaldaContract.JaldaTicksS.Count != 0)
                {
                    int maxPresentJaldaThickSerialNumber = (int)relativeJaldaContract.JaldaTicksS.Max(x => x.Serialnumber);
                    if (maxPresentJaldaThickSerialNumber - jaldaThickPOSTModel.SerialNumber <= -2)
                    {
                        ErrorMessage = "Jalda thicks must increment by one. Or the Jalda Thick with serial number " + (jaldaThickPOSTModel.SerialNumber - 1) + " is probably being missed. ";
                        terminateContract = true;
                        return false;
                    }
                    else if (maxPresentJaldaThickSerialNumber - jaldaThickPOSTModel.SerialNumber >= 2)
                    {
                        ErrorMessage = "Jalda Thick with serial number " + (jaldaThickPOSTModel.SerialNumber + 1) + " is probably being missed";
                        terminateContract = true;
                        return false;
                    }
                }
                //Check not reaching max amount
                if (jaldaThickPOSTModel.JaldaThickType == Constants.JaldaThickTypes.Payment && ((relativeJaldaContract.JaldaTicksS.Where(x => x.Type == Constants.JaldaThickTypes.Payment)).Count() + 1) * relativeJaldaContract.Pricepertick > relativeJaldaContract.Maxamount)
                {
                    ErrorMessage = "Reached Maximum amount in jalda contract";
                    terminateContract = true;
                    return false;
                }

                //No problem found in incoming thick
                ErrorMessage = "";
                terminateContract = false;
                return result;
            }
            catch (Exception ex)
            {
                relativeJaldaContract = null;
                ErrorMessage = "Wrong Jalda Contract ID";
                terminateContract = false;
                return false;
            }
        }

        private bool CheckValidityofPost(CASPaymentDTO.Domain.JaldaContract jaldaContract, out string ErrorMessage)
        {
            ErrorMessage = "";
            return true;
        }

        #endregion

    }
}
