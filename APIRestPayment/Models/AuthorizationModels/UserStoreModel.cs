using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.Identity;

namespace APIRestPayment.AuthorizationModels
{
    public class UserStoreModel : IUserStore<CustomUserModel> , IUserPasswordStore<CustomUserModel>
    {
        CASPaymentDAO.DataHandler.UsersDataHandler usersHandler;

        public UserStoreModel()
        {
            usersHandler = new CASPaymentDAO.DataHandler.UsersDataHandler(WebApiApplication.SessionFactory);
        }

        public async System.Threading.Tasks.Task CreateAsync(CustomUserModel user)
        {
            await Task.Run(() =>
                {
                    usersHandler.Save(user);
                }).ConfigureAwait(false);
        }

        public async System.Threading.Tasks.Task DeleteAsync(CustomUserModel user)
        {
            await Task.Run(() =>
            {
                usersHandler.Delete(user);
            }).ConfigureAwait(false);
        }

        public async System.Threading.Tasks.Task<CustomUserModel> FindByIdAsync(string userId)
        {
           CASPaymentDTO.Domain.Users user = await Task.Run(() =>
            {
                long convertedID = Convert.ToInt64(userId);
                return usersHandler.GetEntity(convertedID);
            }).ConfigureAwait(false);
           return new CustomUserModel(user);
        }

        public async System.Threading.Tasks.Task<CustomUserModel> FindByNameAsync(string userName)
        {
            CASPaymentDTO.Domain.Users user = await Task.Run(() =>
            {
                return usersHandler.Search(new CASPaymentDTO.Domain.Users { Email = userName }).Cast<CASPaymentDTO.Domain.Users>().FirstOrDefault();
            }).ConfigureAwait(false);
            return new CustomUserModel(user);
        }

        public async System.Threading.Tasks.Task UpdateAsync(CustomUserModel user)
        {
            await Task.Run(() =>
            {
                usersHandler.Update(user);
            }).ConfigureAwait(false);
        }

        public void Dispose()
        {
            usersHandler = null;
        }

        public async Task<string> GetPasswordHashAsync(CustomUserModel user)
        {
             string temp =await Task.Run(() =>
            {
                string pp = user.Password.ToString();
                return pp;
            }).ConfigureAwait(false);
             string base64hashpassword = Convert.ToBase64String(Encoding.UTF8.GetBytes(temp));
             return  base64hashpassword ;
        }

        public async Task<bool> HasPasswordAsync(CustomUserModel user)
        {
            return await Task.Run(() =>
            {
                return string.IsNullOrEmpty(user.Password.ToString());
            }).ConfigureAwait(false); 
        }

        public async Task SetPasswordHashAsync(CustomUserModel user, string passwordHash)
        {
             await Task.Run(() =>
            {
                user.Password = passwordHash;
                usersHandler.Update(user);
            }).ConfigureAwait(false); 
        }
    }
}