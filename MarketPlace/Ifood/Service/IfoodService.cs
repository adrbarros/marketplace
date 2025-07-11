﻿using Ifood.Domain;
using Ifood.Domain.Catalog;
using Ifood.Domain.Finance;
using Ifood.Domain.Review;
using Ifood.Utils;
using MarketPlace;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;

namespace Ifood.Service
{
    public class IfoodService
    {
        private string _urlBase = Constants.URL_BASE;

        public IfoodService()
        {
        }

        public IfoodService(string urlBase)
        {
            _urlBase = urlBase;
        }

        /// <summary>
        /// Retorna o token do ifood
        /// </summary>
        /// <param name="client_id"></param>
        /// <param name="client_secret"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public GenericResult<token2> OathToken(bool centralizado, string client_id, string client_secret, string authorizationCode = "", string authorizationCodeVerifier = "", string refreshToken = "")
        {
            var result = new GenericResult<token2>();
            try
            {
                var url = _urlBase + Constants.URL_TOKEN;

                var client = new RestClient(url);
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("clientId", client_id);
                request.AddParameter("clientSecret", client_secret);

                if (centralizado)
                {
                    request.AddParameter("grantType", "client_credentials");
                }
                else
                {
                    if (string.IsNullOrEmpty(refreshToken))
                    {
                        request.AddParameter("grantType", "authorization_code");
                        request.AddParameter("authorizationCode", authorizationCode);
                        request.AddParameter("authorizationCodeVerifier", authorizationCodeVerifier);
                    }
                    else
                    {
                        request.AddParameter("grantType", "refresh_token");
                        request.AddParameter("refreshToken", refreshToken);
                    }
                }

                IRestResponse responseToken = client.Execute(request);

                if (responseToken.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    result.Result = JsonConvert.DeserializeObject<token2>(responseToken.Content);
                    result.Success = true;
                }
                else
                {
                    var error = JsonConvert.DeserializeObject<error_return>(responseToken.Content);
                    result.Message = responseToken.StatusDescription + $" => {error.error.message}";
                }

                result.StatusCode = responseToken.StatusCode;

            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

         

            return result;
        }

        public GenericResult<userCodeResult> UserCode(string client_id)
        {
            var result = new GenericResult<userCodeResult>();
            try
            {
                var url = _urlBase + Constants.URL_CODE;

                var client = new RestClient(url);
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("grantType", "client_credentials");
                request.AddParameter("clientId", client_id);

                IRestResponse responseToken = client.Execute(request);

                if (responseToken.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    result.Result = JsonConvert.DeserializeObject<userCodeResult>(responseToken.Content);
                    result.Success = true;
                }
                else
                {
                    var error = JsonConvert.DeserializeObject<error_return>(responseToken.Content);
                    result.Message = responseToken.StatusDescription + $" => {error.error.message}";
                }
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }


        #region Eventos

        public GenericResult<List<status>> Status(string token, string merchantGuid)
        {
            var result = new GenericResult<List<status>>();

            var url = string.Format("{0}merchant/v1.0/merchants/{1}/status/", _urlBase, merchantGuid);
            var client = new RestClientBase(url);
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", string.Format("Bearer {0}", token));
            IRestResponse response = client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                result.Result = JsonConvert.DeserializeObject<List<status>>(response.Content);
                result.Success = true;
                result.Json = response.Content;
                result.Request = client.requestResult;
                result.Response = client.responsetResult;
            }
            else
            {
                var retorno = JsonConvert.DeserializeObject<error_return>(response.Content);
                if (retorno != null && retorno.error != null)
                    result.Message = retorno.error.message;
                else
                    result.Message = response.Content;
            }

            result.StatusCode = response.StatusCode;
            return result;
        }

        /// <summary>
        /// Obtém todos os eventos ainda não recebidos.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public GenericResult<List<poolingEvent>> EventPolling(string token, string merchantid)
        {
            var result = new GenericResult<List<poolingEvent>>();

            var url = string.Format("{0}order/{1}/{2}", _urlBase, Constants.VERSION_1, Constants.URL_EVENT_POOLING+ "?types=COL,CAN");
            var client = new RestClientBase(url);
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", string.Format("Bearer {0}", token));
            if (!string.IsNullOrEmpty(merchantid))
                request.AddHeader("x-polling-merchants", merchantid);
            IRestResponse response = client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                result.Result = JsonConvert.DeserializeObject<List<poolingEvent>>(response.Content);
                result.Success = true;
                result.Json = response.Content;   

            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound || response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                result.Result = new List<poolingEvent>();
                result.Success = true;
            }
            else
            {
                result.Message = response.StatusDescription;
            }

            result.StatusCode = response.StatusCode;
            result.Request = client.requestResult;
            result.Response = client.responsetResult;

            return result;
        }

        /// <summary>
        /// Após o e-PDV receber os eventos do IFood, para cada evento que o e-PDV conseguiu realizar o parse e integrá-lo com sucesso, o e-PDV deve enviar uma requisição confirmando o recebimento dos eventos.
        /// Recomenda-se que o e-PDV envie uma lista de todos os eventos recebidos com sucesso de uma única vez.Importante salientar que apenas o id do evento é obrigatório.
        /// O IFood processará as notificações e removê-los da fila de eventos do e-PDV.
        /// Na próxima requisição que o e-PDV consulta por novos eventos, os eventos previamente confirmados não farão mais parte da resposta..  
        /// </summary>
        /// <param name="token"></param>
        /// <param name="events"></param>
        /// <returns></returns>
        public GenericSimpleResult EventAcknowledgment(string token, List<eventAcknowledgment> events)
        {
            var result = new GenericSimpleResult();

            var url = string.Format("{0}order/{1}/{2}", _urlBase, Constants.VERSION_1, Constants.URL_EVENT_ACNOWLEDGMENT);
            var client = new RestClient(url);
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", string.Format("Bearer {0}", token));
            request.AddHeader("Content-Type", "application/json");
            request.RequestFormat = DataFormat.Json;
            request.AddBody(events);
            //request.AddParameter("data", events, ParameterType.RequestBody);            
            IRestResponse response = client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                result.Success = true;
            }
            else
            {
                result.Message = response.Content;
            }            

            return result;
        }


        #endregion

        #region Pedido

        /// <summary>
        /// Geralmente, após o e-PDV receber um evento com o código 'PLACED', é necessário obter os detalhes do pedido.
        /// Neste cenário, o campo correlationId do evento refere-se à referência do pedido e deve ser fornecido a este endpoint.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public GenericResult<order> Orders(string token, string reference)
        {
            var result = new GenericResult<order>();
            var url = string.Format("{0}order/{1}/{2}/{3}", _urlBase, Constants.VERSION_1, Constants.URL_ORDER, reference);
            var client = new RestClientBase(url);
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", string.Format("bearer {0}", token));
            var response = client.Execute<RestObject>(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                result.Result = JsonConvert.DeserializeObject<order>(response.Content);
                result.Success = true;
                result.Json = response.Content;
                result.Request = client.requestResult;
                result.Response = client.responsetResult;
            }
            else
            {
                result.Message = response.StatusDescription;
            }

            result.Request = client.requestResult;
            result.Response = client.responsetResult;
            result.StatusCode = response.StatusCode;
            return result;
        }

        ///// <summary>
        ///// Informa ao IFood que o pedido foi integrado pelo e-PDV.
        ///// Integração significa que o e-PDV foi capaz de realizar o parse do pedido e integrar em seu sistema.
        ///// </summary>
        ///// <param name="token"></param>
        ///// <param name="reference"></param>
        ///// <returns></returns>
        //public GenericSimpleResult OrdersIntegration(string token, string reference)
        //{
        //    var data = new { };

        //    var result = new GenericSimpleResult();

        //    var url = string.Format("{0}order/{1}/{2}/{3}/{4}", _urlBase, Constants.VERSION_1, Constants.URL_ORDER, reference, Constants.URL_ORDER_INTEGRATION);
        //    var client = new RestClient(url);
        //    var request = new RestRequest(Method.POST);
        //    request.AddHeader("Authorization", string.Format("bearer {0}", token));
        //    request.AddParameter("application/json", data, ParameterType.RequestBody);
        //    IRestResponse response = client.Execute(request);
        //    if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
        //    {
        //        result.Success = true;
        //    }
        //    else
        //    {
        //        result.Message = response.StatusDescription;
        //    }

        //    return result;
        //}

        /// <summary>
        /// Informa ao IFood que o pedido foi confirmado pelo e-PDV.        
        /// </summary>
        /// <param name="token"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public GenericSimpleResult OrdersConfirmation(string token, string reference)
        {
            var data = new { };
            var result = new GenericSimpleResult();

            var url = string.Format("{0}order/{1}/{2}/{3}/{4}", _urlBase, Constants.VERSION_1, Constants.URL_ORDER, reference, Constants.URL_ORDER_CONFIRM);
            var client = new RestClientBase(url);
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", string.Format("Bearer {0}", token));
            request.AddParameter("application/json", data, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                result.Success = true;
            }
            else
            {
                result.Message = response.StatusDescription;
            }
            result.Request = client.requestResult;
            result.Response = client.responsetResult;
            result.StatusCode = response.StatusCode;

            return result;
        }

        public GenericSimpleResult OrdersStarPreparation(string token, string reference)
        {
            var data = new { };
            var result = new GenericSimpleResult();

            var url = string.Format("{0}order/{1}/{2}/{3}/{4}", _urlBase, Constants.VERSION_1, Constants.URL_ORDER, reference, Constants.URL_ORDER_START_PREPARATION);
            var client = new RestClient(url);
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", string.Format("Bearer {0}", token));
            request.AddParameter("application/json", data, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                result.Success = true;
            }
            else
            {
                result.Message = response.StatusDescription;
            }

            return result;
        }


        /// <summary>
        /// Informa ao IFood que o pedido saiu para ser entregue ao cliente.       
        /// </summary>
        /// <param name="token"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public GenericSimpleResult OrdersDispatch(string token, string reference)
        {
            var data = new { };
            var result = new GenericSimpleResult();

            var url = string.Format("{0}order/{1}/{2}/{3}/{4}", _urlBase, Constants.VERSION_1, Constants.URL_ORDER, reference, Constants.URL_ORDER_DISPATCH);
            var client = new RestClient(url);
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", string.Format("Bearer {0}", token));
            request.AddParameter("application/json", data, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.Accepted || response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                result.Success = true;
            }
            else
            {
                result.Message = response.StatusDescription;
            }
            result.StatusCode = response.StatusCode;

            return result;
        }

        /// <summary>
        /// Informa ao IFood que o pedido foi rejeitado pelo e-PDV.  
        /// </summary>
        /// <param name="token"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public GenericSimpleResult OrdersRejection(string token, string reference)
        {
            var data = new { };
            var result = new GenericSimpleResult();

            var url = string.Format("{0}order/{1}/{2}/{3}/{4}", _urlBase, Constants.VERSION_1, Constants.URL_ORDER, reference, Constants.URL_ORDER_REJECTION);
            var client = new RestClient(url);
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", string.Format("Bearer {0}", token));
            request.AddParameter("application/json", data, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.Accepted || response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                result.Success = true;
            }
            else
            {
                result.Message = response.StatusDescription;
            }
            result.StatusCode = response.StatusCode;

            return result;
        }

        public GenericSimpleResult OrdersReadyToPickup(string token, string reference)
        {
            var data = new { };
            var result = new GenericSimpleResult();

            var url = string.Format("{0}order/{1}/{2}/{3}/{4}", _urlBase, Constants.VERSION_1, Constants.URL_ORDER, reference, Constants.URL_ORDER_READY_TO_PICKUP);
            var client = new RestClient(url);
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", string.Format("Bearer {0}", token));
            request.AddParameter("application/json", data, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.Accepted || response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                result.Success = true;
            }
            else
            {
                result.Message = response.StatusDescription;
            }
            result.StatusCode = response.StatusCode;

            return result;
        }

        #endregion

        #region Cancelamento

        /// <summary>
        /// Solicita o Cancelamento do Pedido
        /// </summary>
        /// <param name="token"></param>
        /// <param name="reference"></param>
        /// <param name="code"></param>
        /// <param name="detail"></param>
        /// <returns></returns>
        public GenericSimpleResult CancellationRequested(string token, string reference, short code, string detail)
        {
            var data = new
            {
                cancellationCode = code.ToString(),
                reason = detail
            };

            var result = new GenericSimpleResult();

            var url = string.Format("{0}order/{1}/{2}/{3}/{4}", _urlBase, Constants.VERSION_1, Constants.URL_ORDER, reference, Constants.URL_ORDER_CANCELATION);
            var client = new RestClient(url);
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", string.Format("Bearer {0}", token));
            request.AddParameter("application/json", JsonConvert.SerializeObject(data), ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.Accepted || response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                result.Success = true;
            }
            else
            {
                if (!string.IsNullOrEmpty(response.Content))
                {
                    try
                    {
                        var resultMessage = JsonConvert.DeserializeObject<error_cancel>(response.Content);
                        result.Message = resultMessage.error.message;
                    }
                    catch { }
                }

                if(string.IsNullOrEmpty(result.Message))
                    result.Message = response.StatusDescription;
            }
            result.StatusCode = response.StatusCode;

            return result;
        }

        /// <summary>
        /// Aceita a solicitação de cancelamento feita pelo cliente
        /// </summary>
        /// <param name="token"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public GenericSimpleResult CancellationAccepted(string token, string reference)
        {
            var data = new { };
            var result = new GenericSimpleResult();

            var url = string.Format("{0}order/{1}/{2}/{3}/{4}", _urlBase, Constants.VERSION_1, Constants.URL_ORDER, reference, Constants.URL_ORDER_CANCELATION_ACCEPTED);
            var client = new RestClient(url);
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", string.Format("Bearer {0}", token));
            request.AddParameter("application/json", data, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.Accepted || response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                result.Success = true;
            }
            else
            {
                result.Message = response.StatusDescription;
            }
            result.StatusCode = response.StatusCode;
            return result;
        }

        /// <summary>
        /// Rejeita a solicitação de cancelamento feita pelo cliente
        /// </summary>
        /// <param name="token"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public GenericSimpleResult CancellationDenied(string token, string reference)
        {
            var data = new { };
            var result = new GenericSimpleResult();

            var url = string.Format("{0}order/{1}/{2}/{3}/{4}", _urlBase, Constants.VERSION_1, Constants.URL_ORDER, reference, Constants.URL_ORDER_CANCELATION_ACCEPTED);
            var client = new RestClient(url);
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", string.Format("Bearer {0}", token));
            request.AddParameter("application/json", data, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.Accepted || response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                result.Success = true;
            }
            else
            {
                result.Message = response.StatusDescription;
            }
            result.StatusCode = response.StatusCode;
            return result;
        }

        #endregion

        #region Financeiro

        public GenericResult<SalesRoot> Sales(string token, string merchantId, DateTime inicio, DateTime fim, int page)
        {
            var result = new GenericResult<SalesRoot>();
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var request = new RestRequest(Method.GET);
                var client = new RestClient($"{Constants.URL_BASE_FINANCE}merchants/{merchantId}/sales");
                client.Timeout = -1;
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddHeader("Authorization", $"Bearer {token}");
                request.AddParameter("beginOrderDate", inicio.ToString("yyyy-MM-dd"));
                request.AddParameter("endOrderDate", fim.ToString("yyyy-MM-dd"));
                request.AddParameter("page", page);
                IRestResponse response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    result.Result = JsonConvert.DeserializeObject<SalesRoot>(response.Content);
                    result.Success = true;
                }
                else
                {
                    result.Message = response.Content;
                }

                result.Json = response.Content;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        public GenericResult<SettlementRoot> Settlements(string token, string merchantId, DateTime inicio, DateTime fim)
        {
            var result = new GenericResult<SettlementRoot>();
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var request = new RestRequest(Method.GET);
                var client = new RestClient($"{Constants.URL_BASE_FINANCE}merchants/{merchantId}/settlements");
                client.Timeout = -1;
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddHeader("Authorization", $"Bearer {token}");
                request.AddParameter("beginPaymentDate", inicio.ToString("yyyy-MM-dd"));
                request.AddParameter("endPaymentDate", fim.ToString("yyyy-MM-dd"));
                IRestResponse response = client.Execute(request);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    result.Result = JsonConvert.DeserializeObject<SettlementRoot>(response.Content);
                    result.Success = true;
                }
                else
                {
                    result.Message = response.Content;
                }

                result.Json = response.Content;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        public GenericResult<Anticipations> Anticipations(string token, string merchantId, DateTime inicio, DateTime fim)
        {
            var result = new GenericResult<Anticipations>();
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var request = new RestRequest(Method.GET);
                var client = new RestClient($"{Constants.URL_BASE_FINANCE}merchants/{merchantId}/anticipations");
                client.Timeout = -1;
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddHeader("Authorization", $"Bearer {token}");
                request.AddParameter("beginAnticipatedPaymentDate", inicio.ToString("yyyy-MM-dd"));
                request.AddParameter("endAnticipatedPaymentDate", fim.ToString("yyyy-MM-dd"));
                IRestResponse response = client.Execute(request);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    result.Result = JsonConvert.DeserializeObject<Anticipations>(response.Content);
                    result.Success = true;
                }
                else
                {
                    result.Message = response.Content;
                }

                result.Json = response.Content;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        public GenericResult<FinancialEventsRoots> FinancialEvents(string token, string merchantId, DateTime inicio, DateTime fim, int page, int size)
        {
            var result = new GenericResult<FinancialEventsRoots>();
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var request = new RestRequest(Method.GET);
                var client = new RestClient($"{Constants.URL_BASE_FINANCE}merchants/{merchantId}/financial-events");
                client.Timeout = -1;
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddHeader("Authorization", $"Bearer {token}");
                request.AddParameter("beginOrderDate", inicio.ToString("yyyy-MM-dd"));
                request.AddParameter("endOrderDate", fim.ToString("yyyy-MM-dd"));
                request.AddParameter("page", page);
                request.AddParameter("size", size);
                IRestResponse response = client.Execute(request);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    result.Result = JsonConvert.DeserializeObject<FinancialEventsRoots>(response.Content);
                    result.Success = true;
                }
                else
                {
                    result.Message = response.Content;
                }

                result.Json = response.Content;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }
       

        #endregion

        #region Review

        public GenericResult<reviewss> Reviews(string token, string merchantId, DateTime inicio, DateTime fim)
        {
            var result = new GenericResult<reviewss>();
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var request = new RestRequest(Method.GET);
                var client = new RestClient($"{Constants.URL_BASE_REVIEW}merchants/{merchantId}/reviews");
                client.Timeout = -1;
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddHeader("Authorization", $"Bearer {token}");
                request.AddParameter("dateFrom", inicio.ToString("yyyy-MM-ddT00:00:00Z"));
                request.AddParameter("dateTo", fim.ToString("yyyy-MM-ddT23:59:00Z"));
                IRestResponse response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {                    
                    result.Result = JsonConvert.DeserializeObject<reviewss>(response.Content);
                    result.Success = true;
                }
                else
                {
                    result.Message = response.Content;
                }

                result.Json = response.Content;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        public GenericResult<review> Review(string token, string merchantId, string reviewId)
        {
            var result = new GenericResult<review>();
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var request = new RestRequest(Method.GET);
                var client = new RestClient($"{Constants.URL_BASE_REVIEW}merchants/{merchantId}/reviews/{reviewId}");
                client.Timeout = -1;
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddHeader("Authorization", $"Bearer {token}");
                IRestResponse response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {                   
                    result.Result = JsonConvert.DeserializeObject<review>(response.Content);
                    result.Success = true;
                }
                else
                {
                    result.Message = response.Content;
                }

                result.Json = response.Content;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        public GenericResult<summary> Summary(string token, string merchantId)
        {
            var result = new GenericResult<summary>();
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var request = new RestRequest(Method.GET);
                var client = new RestClient($"{Constants.URL_BASE_REVIEW}merchants/{merchantId}/summary");
                client.Timeout = -1;
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddHeader("Authorization", $"Bearer {token}");
                IRestResponse response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {                    
                    result.Result = JsonConvert.DeserializeObject<summary>(response.Content);
                    result.Success = true;
                }
                else
                {
                    result.Message = response.Content;
                }

                result.Json = response.Content;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        #endregion

        #region Merchants

        public GenericResult<Ifood.Domain.Merchant.Merchant> Merchants(string token, string merchantId)
        {
            var result = new GenericResult<Ifood.Domain.Merchant.Merchant>();
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var request = new RestRequest(Method.GET);
                var client = new RestClient($"{Constants.URL_BASE}merchants/{merchantId}");
                client.Timeout = -1;
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Authorization", $"Bearer {token}");
                IRestResponse response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    result.Result = JsonConvert.DeserializeObject<Ifood.Domain.Merchant.Merchant>(response.Content);
                    result.Success = true;
                }
                else
                {
                    result.Message = response.Content;
                }

                result.Json = response.Content;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        public GenericResult<Ifood.Domain.Merchant.MerchantInterruptions> Interruptions(string token, string merchantId)
        {
            var result = new GenericResult<Ifood.Domain.Merchant.MerchantInterruptions>();
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var request = new RestRequest(Method.GET);
                var client = new RestClient($"{Constants.URL_BASE}merchants/{merchantId}/interruptions");
                client.Timeout = -1;
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Authorization", $"Bearer {token}");
                IRestResponse response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    result.Result = JsonConvert.DeserializeObject<Ifood.Domain.Merchant.MerchantInterruptions>(response.Content);
                    result.Success = true;
                }
                else
                {
                    result.Message = response.Content;
                }

                result.Json = response.Content;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        public GenericResult<Ifood.Domain.Merchant.MerchantOpeningHours> OpeningHours(string token, string merchantId)
        {
            var result = new GenericResult<Ifood.Domain.Merchant.MerchantOpeningHours>();
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var request = new RestRequest(Method.GET);
                var client = new RestClient($"{Constants.URL_BASE}merchants/{merchantId}/opening-hours");
                client.Timeout = -1;
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Authorization", $"Bearer {token}");
                IRestResponse response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    result.Result = JsonConvert.DeserializeObject<Ifood.Domain.Merchant.MerchantOpeningHours>(response.Content);
                    result.Success = true;
                }
                else
                {
                    result.Message = response.Content;
                }

                result.Json = response.Content;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        #endregion

        #region Catalog

        public GenericResult<List<Ifood.Domain.Catalog.Catalogs>> Catalogs(string token, string merchantId)
        {
            var result = new GenericResult<List<Ifood.Domain.Catalog.Catalogs>>();
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var request = new RestRequest(Method.GET);
                var client = new RestClient($"{Constants.URL_BASE}merchants/{merchantId}/catalogs");
                client.Timeout = -1;
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Authorization", $"Bearer {token}");
                IRestResponse response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    result.Result = JsonConvert.DeserializeObject<List<Ifood.Domain.Catalog.Catalogs>>(response.Content);
                    result.Success = true;
                }
                else
                {
                    result.Message = response.Content;
                }

                result.Json = response.Content;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        public GenericResult<Categories> Categories(string token, string merchantId, string catalogId)
        {
            var result = new GenericResult<Categories>();
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var request = new RestRequest(Method.GET);
                var client = new RestClient($"{Constants.URL_BASE}merchants/{merchantId}/catalogs/{catalogId}/categories");
                client.Timeout = -1;
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Authorization", $"Bearer {token}");
                IRestResponse response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    result.Result = JsonConvert.DeserializeObject<Categories>(response.Content);
                    result.Success = true;
                }
                else
                {
                    result.Message = response.Content;
                }

                result.Json = response.Content;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        public GenericResult<UnsellableItems> UnsellableItems(string token, string merchantId, string catalogId)
        {
            var result = new GenericResult<UnsellableItems>();
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var request = new RestRequest(Method.GET);
                var client = new RestClient($"{Constants.URL_BASE}merchants/{merchantId}/catalogs/{catalogId}/unsellableItems");
                client.Timeout = -1;
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Authorization", $"Bearer {token}");
                IRestResponse response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    result.Result = JsonConvert.DeserializeObject<UnsellableItems>(response.Content);
                    result.Success = true;
                }
                else
                {
                    result.Message = response.Content;
                }

                result.Json = response.Content;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        #endregion

        #region Inventory



        #endregion
    }
}
