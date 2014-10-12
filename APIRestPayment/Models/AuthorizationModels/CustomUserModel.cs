using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.Identity;
using System.Reflection;

namespace APIRestPayment.AuthorizationModels
{
    public class CustomUserModel : CASPaymentDTO.Domain.Users , IUser
    {
        public CustomUserModel(CASPaymentDTO.Domain.Users user)
        {
            InitInhertedProperties(user);
            this.UserName = base.Email;

        }

        private void InitInhertedProperties(object baseClassInstance)
        {
            foreach (PropertyInfo propertyInfo in baseClassInstance.GetType().GetProperties())
            {
                object value = propertyInfo.GetValue(baseClassInstance, null);
                if (null != value) propertyInfo.SetValue(this, value, null);
            }
        }
        public CustomUserModel()
        {
          
        }

        public new string Id
        {
            get { return base.Id.ToString(); }
        }

        public string UserName
        {
            get
            {
               return base.Email;
            }
            set
            {
                this.Email = value;
            }
        }

        //static public explicit operator CustomUserModel(CASPaymentDTO.Domain.Users userDB)
        //{
        //    return new CustomUserModel(userDB);
        //}
    }
}