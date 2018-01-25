namespace CalculateApp
{
    public class Calculate
    {
        public int Add(int Value1, int Value2)
        {
            int temp, temp1 = 0;
            temp1 = Value1 + Value2;
            temp = temp1;
            return temp;
        }
        public int Subtract(int Value1, int Value2)
        {
            int temp, temp1 = 0;
            temp1 = Value1 - Value2;
            temp = temp1;
            return temp;
        }
        public int Multiply(int Value1, int Value2)
        {
            return Value1 * Value2;
        }
        public double Divide(int Value1, int Value2)
        {
            return Value1 / Value2;
        }
        public string Percentage(int Value1, int Value2)
        {
            Value1 = Value1 * 100;
            return Divide(Value1, Value2) + "%";
        }
    }
}