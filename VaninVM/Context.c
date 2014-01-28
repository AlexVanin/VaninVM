#include "Context.h"
#include <malloc.h>
context* create_context(int function, func** hash, char** code)
{
	context* cont;
	cont = (context*)malloc(sizeof(context));

	cont->id = function;
	cont->locals=malloc(8*hash[function]->count_locals);
	*(code)=hash[function]->code;
	return cont;
}

void remove_context(context* cont)
{
	free(cont->locals);
	free(cont);
}

void push_context(context* cont)
{
	c_node* node;
	node = (c_node*)malloc(sizeof(c_node));
	node->obj=cont;
	node->prev = node_last;
	node_last = node;
}
context* pop_context()
{
	c_node* last = node_last;
	node_last = node_last->prev;
	return last->obj;
}

//Find Context in Context Stack.
context* find_context(int id) 
{
	context* result = NULL;
	c_node* current = node_last;
	while (current != NULL || current->prev != NULL)
	{
		if (current->obj->id == id)
		{
			result = current->obj;
			break;
		}
		else
			current = current->prev;
	}
	return result;
}