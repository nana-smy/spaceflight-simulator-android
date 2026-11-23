using System;
using UnityEngine;
// TranslatableName 由 Inspector 序列化赋值
#pragma warning disable 0649

namespace SFS.Translations
{
	[Serializable]
	public class TranslationVariable
	{
		[SerializeField]
		private string TranslatableName;

		[SerializeField]
		public bool plainText;

		private Field field = Field.Text("NULL");

		public Field Field
		{
			get
			{
				if (plainText)
				{
					if (field.subs[0] == "NULL")
					{
						field = Field.Text(TranslatableName);
					}
				}
				else
				{
					Loc.fields.TryGetValue(TranslatableName, out field);
				}
				return field;
			}
			set
			{
				field = value;
			}
		}

		public TranslationVariable()
		{
		}

		public TranslationVariable(Field field)
		{
			this.field = field;
		}
	}
}
