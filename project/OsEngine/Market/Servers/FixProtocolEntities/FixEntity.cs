using System;
using System.Collections.Generic;

namespace OsEngine.Market.Servers.FixProtocolEntities
{
    public class FixEntity
    {
        public List<Field> Fields;

        public FixEntity()
        {
            Fields = new List<Field>();
        }

        public string EntityType
        {
            get { return Fields[2].Value; }
        }


        public void AddField(Field field)
        {
            Fields.Add(field);
        }

        public string GetFieldByTag(int tag)
        {
            string value;

#pragma warning disable CS0168 // Переменная объявлена, но не используется
            try
            {
                value = Fields.Find(field => field.Tag == tag).Value;
            }
            catch (Exception e)
            {
                throw new ArgumentException("Запрошено отсутствующее поле");
            }
#pragma warning restore CS0168 // Переменная объявлена, но не используется

            return value;
        }

    }
}
