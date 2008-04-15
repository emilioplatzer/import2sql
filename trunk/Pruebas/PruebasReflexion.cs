/*
 * Creado por SharpDevelop.
 * Usuario: Emilio
 * Fecha: 22/02/2008
 * Hora: 15:09
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using System.Reflection;
using System.Text;
using NUnit.Framework;

using Comunes;

namespace Pruebas
{
	public class ClaseReflexiva
	{
		public string campo1;
		public int campo2;
		public ClaseReflexiva()
		{
		}
		public override string ToString(){
			return campo1+campo2;
		}
		public string NombresMiembros(){
			StringBuilder rta=new StringBuilder();
			System.Reflection.MemberTypes mt=this.GetType().MemberType;
			System.Reflection.MemberInfo[] ms=this.GetType().GetMembers();
			Separador coma=new Separador(",");
			foreach(MemberInfo m in ms){
				rta.Append(coma+m.Name);
			}
			return rta.ToString();
		}
		public string NombresCampos(){
			StringBuilder rta=new StringBuilder();
			System.Reflection.FieldInfo[] ms=this.GetType().GetFields();
			Separador coma=new Separador(",");
			foreach(FieldInfo m in ms){
				rta.Append(coma+m.Name);
			}
			return rta.ToString();
		}
		public string ValoresCampos(){
			StringBuilder rta=new StringBuilder();
			System.Reflection.FieldInfo[] ms=this.GetType().GetFields();
			Separador coma=new Separador(",");
			foreach(FieldInfo m in ms){
				object o=m.GetValue(this);
				rta.Append(coma+(o==null?"null":o.ToString()));
			}
			return rta.ToString();
		}
		public void PonerValoresPorDefecto(){
			StringBuilder rta=new StringBuilder();
			campo1="pepe";
			System.Reflection.FieldInfo[] ms=this.GetType().GetFields();
			foreach(FieldInfo m in ms){
				if(m.FieldType==typeof(string)){
					m.SetValue(this,m.Name);
				}else{
					m.SetValue(this,m.Name.Length);
				}
			}
		}
	}
	
	[TestFixture]
	public class PruebasReflexion{
		public PruebasReflexion(){
		}
		[Test]
		public void NombresMiembros(){
			ClaseReflexiva r=new ClaseReflexiva();
			Assert.AreEqual("null,0",r.ValoresCampos());
			Assert.AreEqual("campo1,campo2",r.NombresCampos());
			r.PonerValoresPorDefecto();
			Assert.AreEqual("campo1,6",r.ValoresCampos());
		}
	}
}
