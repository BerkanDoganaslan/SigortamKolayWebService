using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Data.OracleClient;
using System.Web.Services.Protocols;
using System.ServiceModel;
using System.Xml;
using System.Data;
using SigortamKolayWebService;

namespace SigortamKolayWebServiceAnkara
{
    
    [WebService(Namespace = "http://SigortamKolay.com/")]  
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    
    public class GetPolicyProposal : System.Web.Services.WebService
    {
        private OracleConnection _dbOperation;
        private OracleConnection _dbOperation2;
        const string FIRM_CODE= "2";
        public AuthHeader user;

        [WebMethod]
        [SoapHeader("user")]
        public output GetProposal(input value)
        {
            OracleCommand cmd = new OracleCommand();
            OracleDataReader dr;
            output returnValue = new output();
            double nTotalCoverAmount = 0;
            double nPremiumRate = 0;
            String commandQuery = "";
            try
            {
                if (user==null)
                    throw new Exception("Header bilgisinde kullanıcı bilgileri gönderilmelidir!");
                if (user.Username != "berkan" || user.Password != "123456")
                    throw new Exception("Kullanıcı adı veya şifresi hatalı!");

                //Ankara sigorta şirketi için tanım varlığı kontrolü yapılıyor.
                cmd.Connection = dbOperation;
                cmd.CommandText = "SELECT * FROM T000FIRMINFO WHERE FIRM_CODE=" + FIRM_CODE;
                dr = cmd.ExecuteReader();
                if (dr.HasRows)
                {
                    returnValue.outList = new List<outputList>();
                    dr.Read();
                    returnValue.insurancefirmName = dr["FIRM_NAME"].ToString();
                }
                else
                {
                    throw new Exception("Firma tanımı bulunamadı!");
                }
                dr.Close();

                //Yaş bilgisine göre prim hesabı yapılıyor.
                cmd.CommandText = "SELECT PREMIUM_AMOUNT FROM T0"+FIRM_CODE+"0PREMIUMAGE WHERE MIN_AGE<=" + value.age + " AND MAX_AGE>=" + value.age;
                dr = cmd.ExecuteReader();
                if (!dr.HasRows)
                {
                    throw new Exception("Yaş bilgisi tanımı bulunamadı!");
                }
                dr.Read();
                returnValue.totalPremiumAmount +=Convert.ToDouble(dr["PREMIUM_AMOUNT"]);
                dr.Close();

                //Şehir bilgisine göre prim hesabı yapılıyor.
                cmd.CommandText = "SELECT PREMIUM_AMOUNT FROM T0"+FIRM_CODE+"0PREMIUMCITY WHERE CITY_CODE=" + value.cityCode;
                dr = cmd.ExecuteReader();
                if (!dr.HasRows)
                {
                    throw new Exception("Şehir bilgisi tanımı bulunamadı!");
                }
                dr.Read();
                returnValue.totalPremiumAmount += Convert.ToDouble(dr["PREMIUM_AMOUNT"]);
                dr.Close();

                //Öğrenim durumu bilgisine göre prim hesabı yapılıyor.
                cmd.CommandText = "SELECT PREMIUM_AMOUNT FROM T0"+FIRM_CODE+"0PREMIUMEDUCATION WHERE EDUCATION_STATUS=" + value.educationStatus;
                dr = cmd.ExecuteReader();
                if (!dr.HasRows)
                {
                    throw new Exception("Öğrenim durumu tanımı bulunamadı!");
                }
                dr.Read();
                returnValue.totalPremiumAmount += Convert.ToDouble(dr["PREMIUM_AMOUNT"]);
                dr.Close();

                //Marka/model bilgisine göre prim hesabı yapılıyor.
                cmd.CommandText = "SELECT PREMIUM_AMOUNT FROM T0" + FIRM_CODE + "0PREMIUMVEHICLEBRAND WHERE BRAND_CODE=" + value.brandCode + " AND MODEL_CODE=" + value.modelCode;
                dr = cmd.ExecuteReader();
                if (!dr.HasRows)
                {
                    throw new Exception("Marka-Model bilgisi tanımı bulunamadı!");
                }
                dr.Read();
                returnValue.totalPremiumAmount += Convert.ToDouble(dr["PREMIUM_AMOUNT"]);
                dr.Close();

                //Model yılı bilgisine göre prim hesabı yapılıyor.
                cmd.CommandText = "SELECT PREMIUM_AMOUNT FROM T0" + FIRM_CODE + "0PREMIUMVEHICLEMODELYEAR WHERE MIN_MODEL_YEAR<=" + value.modelYear + " AND MAX_MODEL_YEAR>=" + value.modelYear;
                dr = cmd.ExecuteReader();
                if (!dr.HasRows)
                {
                    throw new Exception("Model yılı bilgisi tanımı bulunamadı!");
                }
                dr.Read();
                returnValue.totalPremiumAmount += Convert.ToDouble(dr["PREMIUM_AMOUNT"]);
                dr.Close();

                returnValue.clause = System.IO.File.ReadAllText(@"C:\inetpub\wwwroot\SigortamKolayWebService\Ankara\Clause.txt");

                //Poliçe numarası alınıyor.
                cmd.CommandText = "SELECT S011POLICY_NO.NEXTVAL POLICY_NO FROM DUAL";
                dr = cmd.ExecuteReader();
                if (!dr.HasRows)
                {
                    throw new Exception("Poliçe numarası alınamadı!");
                }
                dr.Read();
                returnValue.policyNumber = Convert.ToInt64(dr["POLICY_NO"]);

                //Ürün kodu ve adı set ediliyor. Sistem faz1 aşamasında sadece kasko teklifi verecek şekilde geliştirildi.
                returnValue.productNo = "200";
                returnValue.productName = "Kasko Poliçesi";

                //poliçe başlangıç tarihi set ediliyor.
                returnValue.policyBeginDate = DateTime.Now.ToString("dd/MM/yyyy");
                returnValue.policyEndDate = DateTime.Now.AddYears(1).ToString("dd/MM/yyyy");

                //Poliçede verilecek teminatlar bulunuyor
                cmd.CommandText = "SELECT M.COVER_CODE,M.COVER_NAME,R.COVER_AMOUNT FROM T0"+FIRM_CODE+"1COVERMASTER M, T0"+FIRM_CODE+"1PRODUCTCOVERRATE R" +
                    " WHERE M.COVER_CODE=R.COVER_CODE";
                dr = cmd.ExecuteReader();
                if (!dr.HasRows)
                {
                    throw new Exception("Teminat tanımları bulunamadı!");
                }
                //Prim dağılımını yapmak için toplam teminat bedeli bulunuyor.
                while (dr.Read())
                {
                    nTotalCoverAmount+= Convert.ToDouble(dr["COVER_AMOUNT"]);
                }
                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    nPremiumRate = Convert.ToDouble(dr["COVER_AMOUNT"])/nTotalCoverAmount;
                    returnValue.outList.Add(new outputList()
                    {
                        policyNumber = returnValue.policyNumber,
                        coverCode = Convert.ToInt32(dr["COVER_CODE"]),
                        coverName = dr["COVER_NAME"].ToString(),
                        coverAmount = Convert.ToDouble(dr["COVER_AMOUNT"]),
                        premiumAmount = Math.Round(returnValue.totalPremiumAmount * nPremiumRate, 2)
                    });
                }
  
                //db kayıt işlemleri bir transaction içerisinde burada yapılıyor 
                using (OracleCommand command = new OracleCommand())
                {
                    try
                    {
                        //transaction başlatılıyor
                        command.Connection = dbOperation2;
                        command.Transaction = dbOperation2.BeginTransaction(IsolationLevel.ReadCommitted);

                        int result = 0;

                        //Müşteri kimlik bilgileri kaydediliyor.
                        cmd.CommandText = "SELECT 1 FROM T0" + FIRM_CODE + "1CUSTOMER_MASTER WHERE IDENTITY_TYPE=" + value.identityType +
                        " AND IDENTITY_NO='" + value.identityNo + "'";
                        dr = cmd.ExecuteReader();
                        if (!dr.HasRows)
                        {
                            commandQuery = "INSERT INTO T0" + FIRM_CODE + "1CUSTOMER_MASTER (IDENTITY_TYPE,IDENTITY_NO,NAME,SURNAME,FIRM_NAME) VALUES (:IDENTITY_TYPE,:IDENTITY_NO,:NAME,:SURNAME,:FIRM_NAME)";
                        }
                        else
                        {
                            commandQuery = "UPDATE T0" + FIRM_CODE + "1CUSTOMER_MASTER SET NAME=:NAME,SURNAME=:SURNAME,FIRM_NAME=:FIRM_NAME WHERE IDENTITY_TYPE=:IDENTITY_TYPE AND IDENTITY_NO=:IDENTITY_NO";
                        }
                        command.CommandText = commandQuery;
                        command.Parameters.AddWithValue("IDENTITY_TYPE", value.identityType);
                        command.Parameters.AddWithValue("IDENTITY_NO", value.identityNo);
                        command.Parameters.AddWithValue("NAME", value.name);
                        command.Parameters.AddWithValue("SURNAME", value.surName);
                        if(value.firmName == null)
                            command.Parameters.AddWithValue("FIRM_NAME", DBNull.Value);
                        else 
                            command.Parameters.AddWithValue("FIRM_NAME", value.firmName);
                        result = command.ExecuteNonQuery();
                        if (result < 0)
                            throw new Exception("Müşteri bilgileri kaydedilemedi!");

                        //Diğer Müşteri bilgileri kaydediliyor.
                        cmd.CommandText = "SELECT 1 FROM T0" + FIRM_CODE + "1CUSTOMER_DETAIL WHERE IDENTITY_TYPE=" + value.identityType +
                            " AND IDENTITY_NO='" + value.identityNo + "'";
                        dr = cmd.ExecuteReader();
                        if (!dr.HasRows)
                        {
                            commandQuery = "INSERT INTO T0" + FIRM_CODE + "1CUSTOMER_DETAIL (IDENTITY_TYPE,IDENTITY_NO,AGE,EDUCATION_STATUS) VALUES (:IDENTITY_TYPE,:IDENTITY_NO,:AGE,:EDUCATION_STATUS)";
                        }
                        else
                        {
                            commandQuery = "UPDATE T0" + FIRM_CODE + "1CUSTOMER_DETAIL SET AGE=:AGE,EDUCATION_STATUS=:EDUCATION_STATUS WHERE IDENTITY_TYPE=:IDENTITY_TYPE AND IDENTITY_NO=:IDENTITY_NO";
                        }
                        command.Parameters.Clear();
                        command.CommandText = commandQuery;
                        command.Parameters.AddWithValue("IDENTITY_TYPE", value.identityType);
                        command.Parameters.AddWithValue("IDENTITY_NO", value.identityNo);
                        command.Parameters.AddWithValue("AGE", value.age);
                        command.Parameters.AddWithValue("EDUCATION_STATUS", value.educationStatus);
                        result = command.ExecuteNonQuery();
                        if (result < 0)
                            throw new Exception("Diğer müşteri bilgileri kaydedilemedi!");

                        //Müşteri adres bilgileri kaydediliyor.
                        cmd.CommandText = "SELECT 1 FROM T0" + FIRM_CODE + "1CUSTOMER_ADDRESS WHERE IDENTITY_TYPE=" + value.identityType +
                                               " AND IDENTITY_NO='" + value.identityNo + "'";
                        dr = cmd.ExecuteReader();
                        if (!dr.HasRows)
                        {
                            commandQuery = "INSERT INTO T0" + FIRM_CODE + "1CUSTOMER_ADDRESS (IDENTITY_TYPE,IDENTITY_NO,CITY_CODE,TOWN_NAME,DISTRICT_NAME,VILLAGE_NAME,STREET_NAME,BUILDING_NO,FLOOR_NO,APARTMENT_NO) " +
                                          "VALUES (:IDENTITY_TYPE,:IDENTITY_NO,:CITY_CODE,:TOWN_NAME,:DISTRICT_NAME,:VILLAGE_NAME,:STREET_NAME,:BUILDING_NO,:FLOOR_NO,:APARTMENT_NO)";
                        }
                        else
                        {
                            commandQuery = "UPDATE T0" + FIRM_CODE + "1CUSTOMER_ADDRESS SET CITY_CODE=:CITY_CODE,TOWN_NAME=:TOWN_NAME,DISTRICT_NAME=:DISTRICT_NAME,VILLAGE_NAME=:VILLAGE_NAME,STREET_NAME=:STREET_NAME,BUILDING_NO=:BUILDING_NO,FLOOR_NO=:FLOOR_NO,APARTMENT_NO=:APARTMENT_NO WHERE IDENTITY_TYPE=:IDENTITY_TYPE AND IDENTITY_NO=:IDENTITY_NO";
                        }
                        command.Parameters.Clear();
                        command.CommandText = commandQuery;
                        command.Parameters.AddWithValue("IDENTITY_TYPE", value.identityType);
                        command.Parameters.AddWithValue("IDENTITY_NO", value.identityNo);
                        command.Parameters.AddWithValue("CITY_CODE", value.cityCode);
                        command.Parameters.AddWithValue("TOWN_NAME", value.townCode);
                        command.Parameters.AddWithValue("DISTRICT_NAME", value.districtName);
                        command.Parameters.AddWithValue("VILLAGE_NAME", value.villageName);
                        command.Parameters.AddWithValue("STREET_NAME", value.streetName);
                        command.Parameters.AddWithValue("BUILDING_NO", value.buildingNo);
                        command.Parameters.AddWithValue("FLOOR_NO", value.floorNo);
                        command.Parameters.AddWithValue("APARTMENT_NO", value.apartmentNo);
                        result = command.ExecuteNonQuery();
                        if (result < 0)
                            throw new Exception("Müşteri adres bilgileri kaydedilemedi!");

                        //Araç bilgileri kaydediliyor.
                        cmd.CommandText = "SELECT 1 FROM T0" + FIRM_CODE + "1VEHICLEINFO WHERE IDENTITY_TYPE=" + value.identityType +
                                               " AND IDENTITY_NO='" + value.identityNo + "'";
                        dr = cmd.ExecuteReader();
                        if (!dr.HasRows)
                        {
                            commandQuery = "INSERT INTO T0" + FIRM_CODE + "1VEHICLEINFO (IDENTITY_TYPE,IDENTITY_NO,PLACECITY_CODE,PLATE_NO,BRAND_CODE,MODEL_CODE,MODEL_YEAR) " +
                                          "VALUES (:IDENTITY_TYPE,:IDENTITY_NO,:PLACECITY_CODE,:PLATE_NO,:BRAND_CODE,:MODEL_CODE,:MODEL_YEAR)";
                        }
                        else
                        {
                            commandQuery = "UPDATE T0" + FIRM_CODE + "1VEHICLEINFO SET PLACECITY_CODE=:PLACECITY_CODE,PLATE_NO=:PLATE_NO,BRAND_CODE=:BRAND_CODE,MODEL_CODE=:MODEL_CODE,MODEL_YEAR=:MODEL_YEAR WHERE IDENTITY_TYPE=:IDENTITY_TYPE AND IDENTITY_NO=:IDENTITY_NO";
                        }
                        command.Parameters.Clear();
                        command.CommandText = commandQuery;
                        command.Parameters.AddWithValue("IDENTITY_TYPE", value.identityType);
                        command.Parameters.AddWithValue("IDENTITY_NO", value.identityNo);
                        command.Parameters.AddWithValue("PLACECITY_CODE", value.plateCityCode);
                        command.Parameters.AddWithValue("PLATE_NO", value.plateNo);
                        command.Parameters.AddWithValue("BRAND_CODE", value.brandCode);
                        command.Parameters.AddWithValue("MODEL_CODE", value.modelCode);
                        command.Parameters.AddWithValue("MODEL_YEAR", value.modelYear);
                        result = command.ExecuteNonQuery();
                        if (result < 0)
                            throw new Exception("Müşteri araç bilgileri kaydedilemedi!");

                        //Poliçe bilgileri kaydediliyor.
                        cmd.CommandText = "SELECT 1 FROM T0" + FIRM_CODE + "1POLICY_MASTER WHERE POLICY_NO=" + returnValue.policyNumber +
                            " AND PRODUCT_NO='" + returnValue.productNo + "'";
                        dr = cmd.ExecuteReader();
                        commandQuery = "INSERT INTO T0" + FIRM_CODE + "1POLICY_MASTER (POLICY_NO,PRODUCT_NO,POLICY_BEG_DATE,POLICY_END_DATE,POLICY_ISSUE_DATE,IDENTITY_TYPE,IDENTITY_NO) " +
                            "VALUES (:POLICY_NO,:PRODUCT_NO,:POLICY_BEG_DATE,:POLICY_END_DATE,:POLICY_ISSUE_DATE,:IDENTITY_TYPE,:IDENTITY_NO)";
                        command.Parameters.Clear();
                        command.CommandText = commandQuery;
                        command.Parameters.AddWithValue("POLICY_NO", returnValue.policyNumber);
                        command.Parameters.AddWithValue("PRODUCT_NO", returnValue.productNo);
                        command.Parameters.AddWithValue("POLICY_BEG_DATE", returnValue.policyBeginDate);
                        command.Parameters.AddWithValue("POLICY_END_DATE", returnValue.policyEndDate);
                        command.Parameters.AddWithValue("POLICY_ISSUE_DATE", DateTime.Now.ToString("dd/MM/yyyy"));
                        command.Parameters.AddWithValue("IDENTITY_TYPE", value.identityType);
                        command.Parameters.AddWithValue("IDENTITY_NO", value.identityNo);
                        result = command.ExecuteNonQuery();
                        if (result < 0)
                            throw new Exception("Poliçe bilgileri kaydedilemedi!");                    

                        //Poliçe teminat bilgileri kaydediliyor.
                        cmd.CommandText = "SELECT 1 FROM T0" + FIRM_CODE + "1POLICY_COVER WHERE POLICY_NO=" + returnValue.policyNumber +
                            " AND PRODUCT_NO='" + returnValue.productNo + "'";
                        dr = cmd.ExecuteReader();
                        commandQuery = "INSERT INTO T0" + FIRM_CODE + "1POLICY_COVER (POLICY_NO,PRODUCT_NO,COVER_CODE,COVER_AMOUNT,PREMIUM_AMOUNT) " +
                            "VALUES (:POLICY_NO,:PRODUCT_NO,:COVER_CODE,:COVER_AMOUNT,:PREMIUM_AMOUNT)";
                        command.CommandText = commandQuery;
                        foreach (outputList item in returnValue.outList)
                            {
                                command.Parameters.Clear();
                                command.Parameters.AddWithValue("POLICY_NO", returnValue.policyNumber);
                                command.Parameters.AddWithValue("PRODUCT_NO", returnValue.productNo);
                                command.Parameters.AddWithValue("COVER_CODE", item.coverCode);
                                command.Parameters.AddWithValue("COVER_AMOUNT", item.coverAmount);
                                command.Parameters.AddWithValue("PREMIUM_AMOUNT", item.premiumAmount);
                                result = command.ExecuteNonQuery();
                                if (result < 0)
                                    throw new Exception("Poliçe teminat bilgileri kaydedilemedi!");
                            }
                        //transaction commitleniyor.
                        command.Transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        //transaction rollback ediliyor.
                        command.Transaction.Rollback();
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new SoapException(ex.Message.ToString(),new System.Xml.XmlQualifiedName("InvalidParameter", this.ToString()));
            }
            finally
            {
                if (_dbOperation != null)
                {
                    _dbOperation.Close();
                    _dbOperation.Dispose();
                    _dbOperation = null;
                }
                if (_dbOperation2 != null)
                {
                    _dbOperation2.Close();
                    _dbOperation2.Dispose();
                    _dbOperation2 = null;
                }
            }
            return returnValue;
        }
        //İlçeleri bulundukları Şehirlere göre listeleme yapıyor.
        [WebMethod]
        public List<cityAndTown> GetCitiesAndTowns()
        {
            List<cityAndTown> returnValue = new List<cityAndTown>();
            using (OracleCommand command = new OracleCommand("SELECT C.CITY_CODE,C.CITY_NAME,T.TOWN_CODE,T.TOWN_NAME FROM T000CITY C, T000TOWN T WHERE C.CITY_CODE=T.CITY_CODE ORDER BY C.CITY_CODE,T.TOWN_CODE", dbOperation))
            {
                string oldCityCode = "";
                OracleDataReader dr;
                dr = command.ExecuteReader();
                while (dr.Read())
                {
                    if (oldCityCode != dr["CITY_CODE"].ToString())
                        returnValue.Add(new cityAndTown { cityCode = Convert.ToInt32(dr["CITY_CODE"]), cityName = dr["CITY_NAME"].ToString() });
                    if (returnValue[returnValue.Count - 1].towns == null)
                        returnValue[returnValue.Count - 1].towns = new List<towns>();
                    returnValue[returnValue.Count-1].towns.Add(new towns { townCode = Convert.ToInt32(dr["TOWN_CODE"]), townName = dr["TOWN_NAME"].ToString() });

                    oldCityCode = dr["CITY_CODE"].ToString();
                }

            }

            return returnValue;
        }
        //Araç modellerini bulundukları Markalara göre listeleme yapıyor.
        [WebMethod]
        public List<brandAndModel> GetBrandsAndModels()
        {
            List<brandAndModel> returnValue = new List<brandAndModel>();
            using (OracleCommand command = new OracleCommand("SELECT BRAND_CODE,BRAND_NAME,MODEL_CODE,MODEL_NAME FROM T010VEHICLENAMES", dbOperation))
            {
                string oldBrandCode = "";
                OracleDataReader dr;
                dr = command.ExecuteReader();
                while (dr.Read())
                {
                    if (oldBrandCode != dr["BRAND_CODE"].ToString())
                        returnValue.Add(new brandAndModel { brandCode = Convert.ToInt32(dr["BRAND_CODE"]), brandName = dr["BRAND_NAME"].ToString() });
                    if (returnValue[returnValue.Count - 1].models == null)
                        returnValue[returnValue.Count - 1].models = new List<models>();
                    returnValue[returnValue.Count - 1].models.Add(new models { modelCode = Convert.ToInt32(dr["MODEL_CODE"]), modelName = dr["MODEL_NAME"].ToString() });

                    oldBrandCode = dr["BRAND_CODE"].ToString();
                }

            }

            return returnValue;
        }

        //Veritabanı bağlantısı bu metod da sağlanıyor.
        public OracleConnection dbOperation
        {
            get
            {
                if (_dbOperation == null)
                {
                    _dbOperation = new OracleConnection("Data Source=ORCL;User Id=BERKAN;Password=123456;");
                }
                if (_dbOperation.State != System.Data.ConnectionState.Open)
                {
                    _dbOperation.Open();
                }
                return _dbOperation;
            }
        }
        public OracleConnection dbOperation2
        {
            get
            {
                if (_dbOperation2 == null)
                {
                    _dbOperation2 = new OracleConnection("Data Source=ORCL;User Id=BERKAN;Password=123456;");
                }
                if (_dbOperation2.State != System.Data.ConnectionState.Open)
                {
                    _dbOperation2.Open();
                }
                return _dbOperation2;
            }
        }
    }    
}
