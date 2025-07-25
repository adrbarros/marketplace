﻿using Agilizone.Domain;
using MarketPlace;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agilizone.Service
{
    public class AgilizoneService
    {
        private string _url = "https://api.agilizup.com/agilizone/v2/";

        public AgilizoneService() { }

        public AgilizoneService(string url)
        {
            _url = url;           
        }

        public GenericResult<token> Token(string client_id, string client_secret)
        {
            var result = new GenericResult<token>();
            try
            {
                var data = new token_secret
                {
                    client_id = client_id,
                    client_secret = client_secret
                };

                var client = new RestClient(_url + "oauth/token");
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/json");                               
                request.AddParameter("application/json", JsonConvert.SerializeObject(data), ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                if (response.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    result.Result = JsonConvert.DeserializeObject<token>(response.Content);
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

        public GenericResult<result_order> Order(order order, string token)
        {
            var result = new GenericResult<result_order>();
            try
            {
                var data = new
                {
                    order
                };

                var client = new RestClient(_url + "order");
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", "Bearer " + token);
                request.AddParameter("application/json", JsonConvert.SerializeObject(data), ParameterType.RequestBody);

                IRestResponse response = client.Execute(request);
                if (response.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    result.Result = JsonConvert.DeserializeObject<result_order>(response.Content);
                    if (result.Result.status == null)
                    {
                        result.Success = true;
                    }
                    else
                    {
                        result.Message = result.Result.message;
                    }
                }
                else
                {
                    result.Message = response.Content + " - " + response.StatusDescription;
                }

                result.Json = response.Content;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }
            return result;
        }

        public GenericSimpleResult Status(string number, string token, string status, string description = "")
        {
            var result = new GenericSimpleResult();
            try
            {
                var data = new
                {
                    status,
                    cancellationDetails = new
                    {
                        description
                    }
                };

                var client = new RestClient($"{_url}order/{number}");
                var request = new RestRequest(Method.PATCH);
                request.AddHeader("Authorization", "Bearer " + token);
                request.AddParameter("application/json", JsonConvert.SerializeObject(data), ParameterType.RequestBody);

                IRestResponse response = client.Execute(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {                    
                    result.Success = true;
                }
                else
                {
                    result.Message = response.Content + " - " + response.StatusDescription;
                }

                result.Json = response.Content;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }
            return result;
        }

        public GenericResult<result_order> Order(string number, string token)
        {
            var result = new GenericResult<result_order>();
            try
            {
                var client = new RestClient($"{_url}order/{number}");
                var request = new RestRequest(Method.GET);
                request.AddHeader("Authorization", "Bearer " + token);                
                IRestResponse response = client.Execute(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    result.Result = JsonConvert.DeserializeObject<result_order>(response.Content);
                    result.Success = true;
                }
                else
                {
                    result.Message = response.Content + " - " + response.StatusDescription;
                }

                result.Json = response.Content;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }
            return result;
        }
    }
}
