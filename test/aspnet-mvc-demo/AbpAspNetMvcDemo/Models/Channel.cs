using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
 

namespace AbpAspNetMvcDemo.Controllers
{
    public class Channel
    {/// <summary>
     /// 系统编号,需初始化一条admin
     /// </summary>
        public int SysNo { get; set; }

        /// <summary>
        /// 对应的员工编号
        /// </summary>
        public int EmployeeSysNo { get; set; }

        /// <summary>
        /// 账号
        /// </summary>
        public string UserID { get; set; }


        /// <summary>
        /// 密码
        /// </summary>
        public string Pwd { get; set; }


        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }


        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; }


        /// <summary>
        /// 座机
        /// </summary>
        public string Cell { get; set; }


        /// <summary>
        /// 手机
        /// </summary>
        public string Phone { get; set; }


        /// <summary>
        /// 备注
        /// </summary>
        public string Note { get; set; }



        /// <summary>
        /// 是否是默认的超级管理员
        /// </summary>
        public int IsMaster { get; set; }

    }
}
