using System;
using System.Text;
using System.Text.RegularExpressions;

namespace ANPR
{
    class LicensePlateRegex
    {
        /// <summary>
        /// Process the initial recognition of license plate
        /// </summary>
        /// <param name="licensePlateNumber">Initial license plate</param>
        /// <returns>Initial license plate processed</returns>
        public static string LicensePlateNumberProcess(string licensePlateNumber)
        {
            try
            {
                StringBuilder stringBuilder = new StringBuilder(licensePlateNumber);

                if (char.IsDigit(stringBuilder[0]))
                {
                    stringBuilder.Remove(0, 1);
                }

                licensePlateNumber = stringBuilder.ToString();

                string licensePlateNumberProcessed = "";

                // Get county letters
                string county = CheckCounty(licensePlateNumber);

                // Get numberst after county
                string numbers = CheckNumbers(licensePlateNumber.Remove(0, county.Length));

                // Get last three letters
                string letters = CheckLetters(licensePlateNumber.Remove(0, county.Length).Remove(0, numbers.Length));

                // Get the final license plate number after processing
                licensePlateNumberProcessed = county + numbers + letters;

                // Return final license plate number
                return licensePlateNumberProcessed;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return "";
            }
        }
        private static string CheckCounty(string licensePlateNumber)
        {
            string county = "";

            //string[] countys = { "AB", "AR", "AG", "BC", "BH", "BN", "BT", "BR", "BV", "BZ", "CL", "CS", "CJ", "CT", "CV", "DB", "DJ", "GL", "GR", "GJ", "HR", "HD", "IL", "IS", "IF", "MM", "MH", "MS", "NT", "OT", "PH", "SJ", "SM", "SB", "SV", "TR", "TM", "TL", "VL", "VS", "VN", "B" };

            StringBuilder stringBuilder = new StringBuilder(licensePlateNumber);

            if (!char.IsDigit(stringBuilder[2]))
            {
                stringBuilder.Remove(0, 1);
            }

            if (stringBuilder[0] == '0')
            {
                if (stringBuilder[1] == 'B' || stringBuilder[1] == 'J')
                {
                    stringBuilder[0] = 'D';
                }
                else if (stringBuilder[1] == 'T')
                {
                    stringBuilder[0] = 'O';
                }
            }

            licensePlateNumber = stringBuilder.ToString();

            if (char.IsDigit(licensePlateNumber[1]))
            {
                county = licensePlateNumber[0].ToString();
            }
            else
            {
                county = licensePlateNumber.Substring(0, 2);
            }

            return county;
        }

        private static string CheckNumbers(string licensePlateNumber)
        {
            StringBuilder stringBuilder = new StringBuilder(licensePlateNumber);

            for (int i = 0; i < stringBuilder.Length - 3; i++)
            {
                if (!char.IsDigit(stringBuilder[i]))
                {
                    if (stringBuilder[i] == 'O')
                    {
                        stringBuilder.Remove(i, 1).Insert(i, '0');
                    }
                    else if (stringBuilder[i] == 'D')
                    {
                        stringBuilder.Remove(i, 1).Insert(i, '0');
                    }
                    else if (stringBuilder[i] == 'I')
                    {
                        stringBuilder.Remove(i, 1).Insert(i, 1);
                    }
                }
            }

            return stringBuilder.ToString().Substring(0, licensePlateNumber.Length - 3);
        }

        private static string CheckLetters(string licensePlateNumber)
        {
            StringBuilder stringBuilder = new StringBuilder(licensePlateNumber);

            for (int i = 0; i < stringBuilder.Length; i++)
            {
                if (!char.IsLetter(stringBuilder[i]))
                {
                    if (stringBuilder[i] == '0')
                    {
                        stringBuilder.Remove(i, 1).Insert(i, 'O');
                    }
                    else if (stringBuilder[i] == '1')
                    {
                        stringBuilder.Remove(i, 1).Insert(i, 'I');
                    }
                }
            }

            return stringBuilder.ToString();
        }

        public static bool MatchRegex(string licensePlateNubmer)
        {
            string[] patterns = { "^[A-Z][A-Z][0-9][0-9][A-Z][A-Z][A-Z]$", "^[A-Z][A-Z][0-9][0-9][0-9][A-Z][A-Z][A-Z]$", "^[B][0-9][0-9][A-Z][A-Z][A-Z]$", "^[B][0-9][0-9][0-9][A-Z][A-Z][A-Z]$", "^[A-Z][A-Z][0-9][0-9][0-9][0-9][0-9][0-9]$", "^[B[0-9][0-9][0-9][0-9][0-9][0-9]$" };

            Regex regex;

            bool regexMatch = false;

            foreach (var pattern in patterns)
            {
                regex = new Regex(pattern);

                regexMatch = regex.IsMatch(licensePlateNubmer);

                if (regexMatch)
                {
                    break;
                }
            }

            return regexMatch;
        }
    }
}
