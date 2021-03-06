/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 03/05/2008
 * Time: 11:14 a.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Text;
using NUnit.Framework;

namespace Comunes
{
	public class Diccionario<TKey, TValue>:System.Collections.Generic.Dictionary<TKey, TValue>{};
	public class Lista<T>:System.Collections.Generic.List<T>{
		public Lista(){}
		public Lista(T UnicoElemento){
			this.Add(UnicoElemento);
		}
		public override string ToString(){
			StringBuilder rta=new StringBuilder("Lista<"+typeof(T).Name+">=[");
			Separador coma=new Separador(",");
			foreach(T elemento in this){
				rta.Append(coma+elemento.ToString());
			}
			rta.Append("]");
			return rta.ToString();
		}
	};
	public class Conjunto<T>:System.Collections.Generic.Dictionary<T, int>{
		public Conjunto(){}
		public Conjunto(T t){ this.Add(t); }
		public Conjunto(Conjunto<T> t){ this.AddRange(t); }
		Conjunto<T> AddAdd(T t,int cuanto){
			if(this.ContainsKey(t)){
				this[t]+=cuanto;
			}else{
				this.Add(t,cuanto);
			}
			return this;
		}
		public Conjunto<T> Add(T t){
			return AddAdd(t,1);
		}
		public Conjunto<T> AddRange(Conjunto<T> conj){
			foreach(System.Collections.Generic.KeyValuePair<T, int> t in conj){
				this.AddAdd(t.Key,t.Value);
			}
			return this;
		}
		public Conjunto<T> AddRange(params T[] conj){
			foreach(T t in conj){
				this.AddAdd(t,1);
			}
			return this;
		}
		public bool Contiene(T t){
			if(t==null) {
				return false;
			}
			return ContainsKey(t);
		}
		public bool ContieneTodas(Conjunto<T> conj){
			bool rta=true;
			foreach(var t in conj.Keys){
				rta&=Contiene(t);
			}
			return rta;
		}
		public override string ToString(){
			StringBuilder rta=new StringBuilder("<");
			Separador coma=new Separador("; ");
			foreach(System.Collections.Generic.KeyValuePair<T, int> t in this){
				rta.Append(coma+t.Key.ToString());
			}
			return rta.ToString()+">";
		}
		public T UnicoElemento(){
			if(Count!=1){
				Falla.Detener("Se esperaba que el conjunto tuviera un �nico elemento");
			}
			foreach(var par in this){
				return par.Key;
			}
			return default(T);
		}
	}
	[TestFixture]
	public class prConjunto{
		[Test]
		public void probar(){
			Conjunto<string> colores=new Conjunto<string>();
			colores.AddRange("Rojo","Verde","Azul");
			Assert.IsTrue(colores.Contiene("Verde"));
			Conjunto<string> otroscolores=new Conjunto<string>();
			otroscolores.AddRange("Amarillo","Naranja");
			colores.AddRange(otroscolores);
			Assert.AreEqual("<Rojo; Verde; Azul; Amarillo; Naranja>",colores.ToString());
		}
	}
}
